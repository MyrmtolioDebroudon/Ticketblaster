using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TicketBlaster.Server.Services;
using TicketBlaster.Shared.Models;
using System.Security.Claims;

namespace TicketBlaster.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [ApiVersion("1.0")]
    public class EventDiscoveryController : ControllerBase
    {
        private readonly IEventDiscoveryService _discoveryService;
        private readonly ILogger<EventDiscoveryController> _logger;

        public EventDiscoveryController(
            IEventDiscoveryService discoveryService,
            ILogger<EventDiscoveryController> logger)
        {
            _discoveryService = discoveryService;
            _logger = logger;
        }

        /// <summary>
        /// Get personalized discovery content
        /// </summary>
        /// <param name="latitude">User's latitude (optional)</param>
        /// <param name="longitude">User's longitude (optional)</param>
        /// <returns>Discovery sections with recommended events</returns>
        [HttpGet("")]
        [ProducesResponseType(typeof(EventDiscoveryResponse), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetDiscoveryContent(
            [FromQuery] double? latitude = null,
            [FromQuery] double? longitude = null)
        {
            try
            {
                var request = new EventDiscoveryRequest
                {
                    Latitude = latitude,
                    Longitude = longitude
                };

                // Get user ID if authenticated
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                    {
                        request.UserId = userId;
                    }
                }

                var response = await _discoveryService.GetDiscoveryContentAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting discovery content");
                return StatusCode(500, new { message = "An error occurred while getting discovery content" });
            }
        }

        /// <summary>
        /// Get trending events
        /// </summary>
        /// <param name="categoryId">Filter by category (optional)</param>
        /// <param name="period">Time period for trending calculation</param>
        /// <param name="count">Number of events to return</param>
        /// <returns>List of trending events</returns>
        [HttpGet("trending")]
        [ProducesResponseType(typeof(List<EventSearchResult>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTrendingEvents(
            [FromQuery] int? categoryId = null,
            [FromQuery] TrendingPeriod period = TrendingPeriod.Last7Days,
            [FromQuery] int count = 10)
        {
            try
            {
                if (count < 1 || count > 50)
                    count = 10;

                var request = new TrendingEventsRequest
                {
                    CategoryId = categoryId,
                    Period = period,
                    Count = count
                };

                var events = await _discoveryService.GetTrendingEventsAsync(request);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trending events");
                return StatusCode(500, new { message = "An error occurred while getting trending events" });
            }
        }

        /// <summary>
        /// Get personalized event recommendations
        /// </summary>
        /// <param name="count">Number of recommendations</param>
        /// <param name="type">Recommendation algorithm type</param>
        /// <returns>List of recommended events</returns>
        [HttpGet("recommended")]
        [Authorize]
        [ProducesResponseType(typeof(List<EventSearchResult>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetRecommendedEvents(
            [FromQuery] int count = 10,
            [FromQuery] RecommendationType type = RecommendationType.Hybrid)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                if (count < 1 || count > 50)
                    count = 10;

                var request = new EventRecommendationRequest
                {
                    UserId = userId,
                    Count = count,
                    Type = type
                };

                var events = await _discoveryService.GetRecommendedEventsAsync(request);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommended events");
                return StatusCode(500, new { message = "An error occurred while getting recommended events" });
            }
        }

        /// <summary>
        /// Get events similar to a specific event
        /// </summary>
        /// <param name="eventId">Event ID to find similar events for</param>
        /// <param name="count">Number of similar events to return</param>
        /// <returns>List of similar events</returns>
        [HttpGet("similar/{eventId}")]
        [ProducesResponseType(typeof(List<EventSearchResult>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetSimilarEvents(int eventId, [FromQuery] int count = 5)
        {
            try
            {
                if (count < 1 || count > 20)
                    count = 5;

                var events = await _discoveryService.GetSimilarEventsAsync(eventId, count);
                
                if (!events.Any())
                {
                    return NotFound(new { message = "Event not found or no similar events available" });
                }

                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting similar events for EventId: {EventId}", eventId);
                return StatusCode(500, new { message = "An error occurred while getting similar events" });
            }
        }

        /// <summary>
        /// Get event statistics
        /// </summary>
        /// <param name="eventId">Event ID</param>
        /// <returns>Event statistics including views, sales, and trends</returns>
        [HttpGet("statistics/{eventId}")]
        [Authorize(Policy = "OrganizerOnly")]
        [ProducesResponseType(typeof(EventStatisticsResponse), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetEventStatistics(int eventId)
        {
            try
            {
                // TODO: Verify that the user is the organizer of this event
                
                var stats = await _discoveryService.GetEventStatisticsAsync(eventId);
                
                if (stats == null)
                {
                    return NotFound(new { message = "Event not found" });
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event statistics for EventId: {EventId}", eventId);
                return StatusCode(500, new { message = "An error occurred while getting event statistics" });
            }
        }

        /// <summary>
        /// Record event view
        /// </summary>
        /// <param name="eventId">Event ID</param>
        /// <returns>Success status</returns>
        [HttpPost("view/{eventId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> RecordEventView(int eventId)
        {
            try
            {
                int? userId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var id))
                    {
                        userId = id;
                    }
                }

                await _discoveryService.RecordEventViewAsync(eventId, userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording event view for EventId: {EventId}", eventId);
                return StatusCode(500, new { message = "An error occurred while recording event view" });
            }
        }

        /// <summary>
        /// Record event interaction (save, share, etc.)
        /// </summary>
        /// <param name="eventId">Event ID</param>
        /// <param name="interactionType">Type of interaction (save, share, etc.)</param>
        /// <returns>Success status</returns>
        [HttpPost("interact/{eventId}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> RecordEventInteraction(int eventId, [FromQuery] string interactionType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(interactionType))
                {
                    return BadRequest(new { message = "Interaction type is required" });
                }

                var validInteractionTypes = new[] { "save", "share", "like", "interested", "going" };
                if (!validInteractionTypes.Contains(interactionType.ToLower()))
                {
                    return BadRequest(new { message = "Invalid interaction type" });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                await _discoveryService.RecordEventInteractionAsync(eventId, userId, interactionType);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording event interaction for EventId: {EventId}, Type: {Type}", 
                    eventId, interactionType);
                return StatusCode(500, new { message = "An error occurred while recording event interaction" });
            }
        }

        /// <summary>
        /// Get featured events
        /// </summary>
        /// <param name="count">Number of events to return</param>
        /// <returns>List of featured events</returns>
        [HttpGet("featured")]
        [ProducesResponseType(typeof(List<EventSearchResult>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetFeaturedEvents([FromQuery] int count = 10)
        {
            try
            {
                if (count < 1 || count > 50)
                    count = 10;

                var request = new EventSearchRequest
                {
                    IsFeatured = true,
                    PageSize = count,
                    SortBy = EventSortBy.StartDate
                };

                // Use trending service to get featured events
                var trendingRequest = new TrendingEventsRequest { Count = count };
                var events = await _discoveryService.GetTrendingEventsAsync(trendingRequest);
                
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting featured events");
                return StatusCode(500, new { message = "An error occurred while getting featured events" });
            }
        }

        /// <summary>
        /// Get events happening this week
        /// </summary>
        /// <param name="count">Number of events to return</param>
        /// <returns>List of events happening this week</returns>
        [HttpGet("this-week")]
        [ProducesResponseType(typeof(List<EventSearchResult>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetEventsThisWeek([FromQuery] int count = 10)
        {
            try
            {
                if (count < 1 || count > 50)
                    count = 10;

                var discovery = await _discoveryService.GetDiscoveryContentAsync(new EventDiscoveryRequest());
                var thisWeekSection = discovery.Sections
                    .FirstOrDefault(s => s.Type == DiscoverySectionType.UpcomingThisWeek);

                if (thisWeekSection != null)
                {
                    return Ok(thisWeekSection.Events.Take(count));
                }

                return Ok(new List<EventSearchResult>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events this week");
                return StatusCode(500, new { message = "An error occurred while getting events this week" });
            }
        }
    }
}