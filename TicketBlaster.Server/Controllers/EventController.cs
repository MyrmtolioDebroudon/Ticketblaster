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
    public class EventController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly IEventDiscoveryService _discoveryService;
        private readonly ILogger<EventController> _logger;

        public EventController(
            IEventService eventService,
            IEventDiscoveryService discoveryService,
            ILogger<EventController> logger)
        {
            _eventService = eventService;
            _discoveryService = discoveryService;
            _logger = logger;
        }

        /// <summary>
        /// Get event by ID
        /// </summary>
        /// <param name="id">Event ID</param>
        /// <returns>Event details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Event), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetEvent(int id)
        {
            var eventEntity = await _eventService.GetEventAsync(id);
            if (eventEntity == null)
            {
                return NotFound(new { message = "Event not found" });
            }

            // Record event view
            int? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var uid))
                {
                    userId = uid;
                }
            }
            await _discoveryService.RecordEventViewAsync(id, userId);

            return Ok(eventEntity);
        }

        /// <summary>
        /// Get event by custom URL
        /// </summary>
        /// <param name="customUrl">Custom URL</param>
        /// <returns>Event details</returns>
        [HttpGet("url/{customUrl}")]
        [ProducesResponseType(typeof(Event), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetEventByUrl(string customUrl)
        {
            var eventEntity = await _eventService.GetEventByCustomUrlAsync(customUrl);
            if (eventEntity == null)
            {
                return NotFound(new { message = "Event not found" });
            }

            return Ok(eventEntity);
        }

        /// <summary>
        /// Get public events with pagination
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="size">Page size</param>
        /// <returns>List of public events</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Event>), 200)]
        public async Task<IActionResult> GetPublicEvents([FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            if (page < 1) page = 1;
            if (size < 1 || size > 100) size = 20;

            var events = await _eventService.GetPublicEventsAsync(page, size);
            return Ok(events);
        }

        /// <summary>
        /// Get events by category
        /// </summary>
        /// <param name="categoryId">Category ID</param>
        /// <param name="page">Page number</param>
        /// <param name="size">Page size</param>
        /// <returns>List of events in category</returns>
        [HttpGet("category/{categoryId}")]
        [ProducesResponseType(typeof(IEnumerable<Event>), 200)]
        public async Task<IActionResult> GetEventsByCategory(int categoryId, [FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            if (page < 1) page = 1;
            if (size < 1 || size > 100) size = 20;

            var events = await _eventService.GetEventsByCategoryAsync(categoryId, page, size);
            return Ok(events);
        }

        /// <summary>
        /// Get featured events
        /// </summary>
        /// <param name="count">Number of events to return</param>
        /// <returns>List of featured events</returns>
        [HttpGet("featured")]
        [ProducesResponseType(typeof(IEnumerable<Event>), 200)]
        public async Task<IActionResult> GetFeaturedEvents([FromQuery] int count = 10)
        {
            if (count < 1 || count > 50) count = 10;

            var events = await _eventService.GetFeaturedEventsAsync(count);
            return Ok(events);
        }

        /// <summary>
        /// Get organizer's events
        /// </summary>
        /// <returns>List of events for the authenticated organizer</returns>
        [HttpGet("my-events")]
        [Authorize(Policy = "OrganizerOnly")]
        [ProducesResponseType(typeof(IEnumerable<Event>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetMyEvents()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            var events = await _eventService.GetEventsAsync(userId);
            return Ok(events);
        }

        /// <summary>
        /// Create a new event
        /// </summary>
        /// <param name="eventModel">Event to create</param>
        /// <returns>Created event</returns>
        [HttpPost]
        [Authorize(Policy = "OrganizerOnly")]
        [ProducesResponseType(typeof(Event), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> CreateEvent([FromBody] Event eventModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            eventModel.OrganizerId = userId;
            eventModel.CreatedBy = userId;
            eventModel.Status = EventStatus.Draft;

            try
            {
                var createdEvent = await _eventService.CreateEventAsync(eventModel);
                return CreatedAtAction(nameof(GetEvent), new { id = createdEvent.EventId }, createdEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");
                return StatusCode(500, new { message = "An error occurred while creating the event" });
            }
        }

        /// <summary>
        /// Update an event
        /// </summary>
        /// <param name="id">Event ID</param>
        /// <param name="eventModel">Updated event data</param>
        /// <returns>Updated event</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "OrganizerOnly")]
        [ProducesResponseType(typeof(Event), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] Event eventModel)
        {
            if (id != eventModel.EventId)
            {
                return BadRequest(new { message = "Event ID mismatch" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            // Verify user is the organizer
            if (!await _eventService.IsEventOrganizerAsync(id, userId))
            {
                return Forbid("You are not authorized to update this event");
            }

            eventModel.UpdatedBy = userId;

            try
            {
                var updatedEvent = await _eventService.UpdateEventAsync(eventModel);
                return Ok(updatedEvent);
            }
            catch (InvalidOperationException)
            {
                return NotFound(new { message = "Event not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event {EventId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the event" });
            }
        }

        /// <summary>
        /// Delete an event
        /// </summary>
        /// <param name="id">Event ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "OrganizerOnly")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            // Verify user is the organizer
            if (!await _eventService.IsEventOrganizerAsync(id, userId))
            {
                return Forbid("You are not authorized to delete this event");
            }

            try
            {
                var result = await _eventService.DeleteEventAsync(id);
                if (!result)
                {
                    return NotFound(new { message = "Event not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event {EventId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the event" });
            }
        }

        /// <summary>
        /// Publish an event
        /// </summary>
        /// <param name="id">Event ID</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/publish")]
        [Authorize(Policy = "OrganizerOnly")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PublishEvent(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            // Verify user is the organizer
            if (!await _eventService.IsEventOrganizerAsync(id, userId))
            {
                return Forbid("You are not authorized to publish this event");
            }

            try
            {
                var result = await _eventService.PublishEventAsync(id);
                if (!result)
                {
                    return NotFound(new { message = "Event not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing event {EventId}", id);
                return StatusCode(500, new { message = "An error occurred while publishing the event" });
            }
        }

        /// <summary>
        /// Cancel an event
        /// </summary>
        /// <param name="id">Event ID</param>
        /// <returns>Success status</returns>
        [HttpPost("{id}/cancel")]
        [Authorize(Policy = "OrganizerOnly")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CancelEvent(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            // Verify user is the organizer
            if (!await _eventService.IsEventOrganizerAsync(id, userId))
            {
                return Forbid("You are not authorized to cancel this event");
            }

            try
            {
                var result = await _eventService.CancelEventAsync(id);
                if (!result)
                {
                    return NotFound(new { message = "Event not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling event {EventId}", id);
                return StatusCode(500, new { message = "An error occurred while cancelling the event" });
            }
        }

        /// <summary>
        /// Get event statistics
        /// </summary>
        /// <param name="id">Event ID</param>
        /// <returns>Event statistics</returns>
        [HttpGet("{id}/statistics")]
        [Authorize(Policy = "OrganizerOnly")]
        [ProducesResponseType(typeof(EventStatistics), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetEventStatistics(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            // Verify user is the organizer
            if (!await _eventService.IsEventOrganizerAsync(id, userId))
            {
                return Forbid("You are not authorized to view statistics for this event");
            }

            try
            {
                var stats = await _eventService.GetEventStatisticsAsync(id);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event statistics for {EventId}", id);
                return StatusCode(500, new { message = "An error occurred while getting event statistics" });
            }
        }
    }
}