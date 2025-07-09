using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TicketBlaster.Server.Services;
using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [ApiVersion("1.0")]
    public class EventSearchController : ControllerBase
    {
        private readonly IEventSearchService _searchService;
        private readonly ILogger<EventSearchController> _logger;

        public EventSearchController(
            IEventSearchService searchService,
            ILogger<EventSearchController> logger)
        {
            _searchService = searchService;
            _logger = logger;
        }

        /// <summary>
        /// Search events with advanced filtering and sorting options
        /// </summary>
        /// <param name="request">Search request parameters</param>
        /// <returns>Paginated search results with facets</returns>
        [HttpPost("search")]
        [ProducesResponseType(typeof(EventSearchResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SearchEvents([FromBody] EventSearchRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validate pagination parameters
                if (request.PageNumber < 1)
                    request.PageNumber = 1;
                if (request.PageSize < 1 || request.PageSize > 100)
                    request.PageSize = 20;

                var response = await _searchService.SearchEventsAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching events");
                return StatusCode(500, new { message = "An error occurred while searching events" });
            }
        }

        /// <summary>
        /// Get search facets for filtering
        /// </summary>
        /// <param name="request">Optional filters to apply</param>
        /// <returns>Available facets for search refinement</returns>
        [HttpPost("facets")]
        [ProducesResponseType(typeof(SearchFacets), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetSearchFacets([FromBody] EventSearchRequest? request = null)
        {
            try
            {
                request ??= new EventSearchRequest();
                var facets = await _searchService.GetSearchFacetsAsync(request);
                return Ok(facets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search facets");
                return StatusCode(500, new { message = "An error occurred while getting search facets" });
            }
        }

        /// <summary>
        /// Get popular search terms
        /// </summary>
        /// <param name="count">Number of terms to return (default: 10)</param>
        /// <returns>List of popular search terms with statistics</returns>
        [HttpGet("popular-terms")]
        [ProducesResponseType(typeof(PopularSearchTerms), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetPopularSearchTerms([FromQuery] int count = 10)
        {
            try
            {
                if (count < 1 || count > 50)
                    count = 10;

                var terms = await _searchService.GetPopularSearchTermsAsync(count);
                return Ok(terms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular search terms");
                return StatusCode(500, new { message = "An error occurred while getting popular search terms" });
            }
        }

        /// <summary>
        /// Quick search with minimal parameters
        /// </summary>
        /// <param name="q">Search query</param>
        /// <param name="categoryId">Optional category filter</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="size">Page size (default: 20)</param>
        /// <returns>Search results</returns>
        [HttpGet("quick")]
        [ProducesResponseType(typeof(EventSearchResponse), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> QuickSearch(
            [FromQuery] string? q = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] int page = 1,
            [FromQuery] int size = 20)
        {
            try
            {
                var request = new EventSearchRequest
                {
                    SearchTerm = q,
                    CategoryId = categoryId,
                    PageNumber = page,
                    PageSize = size,
                    SortBy = EventSortBy.Relevance
                };

                var response = await _searchService.SearchEventsAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick search");
                return StatusCode(500, new { message = "An error occurred during quick search" });
            }
        }

        /// <summary>
        /// Search events by location
        /// </summary>
        /// <param name="latitude">User's latitude</param>
        /// <param name="longitude">User's longitude</param>
        /// <param name="radius">Search radius in kilometers (default: 50)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="size">Page size (default: 20)</param>
        /// <returns>Events near the specified location</returns>
        [HttpGet("nearby")]
        [ProducesResponseType(typeof(EventSearchResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SearchNearbyEvents(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] double radius = 50,
            [FromQuery] int page = 1,
            [FromQuery] int size = 20)
        {
            try
            {
                if (latitude < -90 || latitude > 90)
                    return BadRequest(new { message = "Invalid latitude. Must be between -90 and 90." });

                if (longitude < -180 || longitude > 180)
                    return BadRequest(new { message = "Invalid longitude. Must be between -180 and 180." });

                if (radius <= 0 || radius > 500)
                    return BadRequest(new { message = "Invalid radius. Must be between 0 and 500 km." });

                var request = new EventSearchRequest
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    RadiusKm = radius,
                    PageNumber = page,
                    PageSize = size,
                    SortBy = EventSortBy.Distance
                };

                var response = await _searchService.SearchEventsAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching nearby events");
                return StatusCode(500, new { message = "An error occurred while searching nearby events" });
            }
        }

        /// <summary>
        /// Search events by date range
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="size">Page size (default: 20)</param>
        /// <returns>Events within the date range</returns>
        [HttpGet("by-date")]
        [ProducesResponseType(typeof(EventSearchResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SearchEventsByDate(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int page = 1,
            [FromQuery] int size = 20)
        {
            try
            {
                if (endDate < startDate)
                    return BadRequest(new { message = "End date must be after start date." });

                var request = new EventSearchRequest
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    PageNumber = page,
                    PageSize = size,
                    SortBy = EventSortBy.StartDate
                };

                var response = await _searchService.SearchEventsAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching events by date");
                return StatusCode(500, new { message = "An error occurred while searching events by date" });
            }
        }

        /// <summary>
        /// Search events by price range
        /// </summary>
        /// <param name="minPrice">Minimum price (inclusive)</param>
        /// <param name="maxPrice">Maximum price (inclusive)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="size">Page size (default: 20)</param>
        /// <returns>Events within the price range</returns>
        [HttpGet("by-price")]
        [ProducesResponseType(typeof(EventSearchResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SearchEventsByPrice(
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] int page = 1,
            [FromQuery] int size = 20)
        {
            try
            {
                if (minPrice.HasValue && minPrice.Value < 0)
                    return BadRequest(new { message = "Minimum price cannot be negative." });

                if (maxPrice.HasValue && maxPrice.Value < 0)
                    return BadRequest(new { message = "Maximum price cannot be negative." });

                if (minPrice.HasValue && maxPrice.HasValue && maxPrice.Value < minPrice.Value)
                    return BadRequest(new { message = "Maximum price must be greater than minimum price." });

                var request = new EventSearchRequest
                {
                    MinPrice = minPrice,
                    MaxPrice = maxPrice,
                    PageNumber = page,
                    PageSize = size,
                    SortBy = EventSortBy.Price
                };

                var response = await _searchService.SearchEventsAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching events by price");
                return StatusCode(500, new { message = "An error occurred while searching events by price" });
            }
        }

        /// <summary>
        /// Search virtual events
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="size">Page size (default: 20)</param>
        /// <returns>Virtual events</returns>
        [HttpGet("virtual")]
        [ProducesResponseType(typeof(EventSearchResponse), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SearchVirtualEvents(
            [FromQuery] int page = 1,
            [FromQuery] int size = 20)
        {
            try
            {
                var request = new EventSearchRequest
                {
                    IsVirtual = true,
                    PageNumber = page,
                    PageSize = size,
                    SortBy = EventSortBy.StartDate
                };

                var response = await _searchService.SearchEventsAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching virtual events");
                return StatusCode(500, new { message = "An error occurred while searching virtual events" });
            }
        }

        /// <summary>
        /// Search free events
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="size">Page size (default: 20)</param>
        /// <returns>Free events</returns>
        [HttpGet("free")]
        [ProducesResponseType(typeof(EventSearchResponse), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SearchFreeEvents(
            [FromQuery] int page = 1,
            [FromQuery] int size = 20)
        {
            try
            {
                var request = new EventSearchRequest
                {
                    MaxPrice = 0,
                    PageNumber = page,
                    PageSize = size,
                    SortBy = EventSortBy.StartDate
                };

                var response = await _searchService.SearchEventsAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching free events");
                return StatusCode(500, new { message = "An error occurred while searching free events" });
            }
        }
    }
}