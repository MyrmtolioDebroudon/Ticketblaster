using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TicketBlaster.Database;
using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public interface IEventSearchService
    {
        Task<EventSearchResponse> SearchEventsAsync(EventSearchRequest request);
        Task<SearchFacets> GetSearchFacetsAsync(EventSearchRequest request);
        Task<PopularSearchTerms> GetPopularSearchTermsAsync(int count = 10);
        Task LogSearchTermAsync(string searchTerm, int? userId = null);
    }

    public class EventSearchService : IEventSearchService
    {
        private readonly TicketBlasterDbContext _context;
        private readonly ILogger<EventSearchService> _logger;

        public EventSearchService(TicketBlasterDbContext context, ILogger<EventSearchService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<EventSearchResponse> SearchEventsAsync(EventSearchRequest request)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Start with base query
                var query = _context.Events
                    .Include(e => e.Category)
                    .Include(e => e.Organizer)
                    .Include(e => e.TicketTypes)
                    .Include(e => e.Orders)
                    .Where(e => e.IsActive);

                // Apply status filter (default to Published only for public search)
                if (request.Status.HasValue)
                {
                    query = query.Where(e => e.Status == request.Status.Value);
                }
                else
                {
                    query = query.Where(e => e.Status == EventStatus.Published);
                }

                // Apply text search
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchTermLower = request.SearchTerm.ToLower();
                    query = query.Where(e =>
                        e.Title.ToLower().Contains(searchTermLower) ||
                        e.Description.ToLower().Contains(searchTermLower) ||
                        e.Location.ToLower().Contains(searchTermLower) ||
                        e.Tags.ToLower().Contains(searchTermLower));

                    // Log search term for trending
                    await LogSearchTermAsync(request.SearchTerm);
                }

                // Apply category filter
                if (request.CategoryIds?.Any() == true)
                {
                    query = query.Where(e => request.CategoryIds.Contains(e.CategoryId));
                }
                else if (request.CategoryId.HasValue)
                {
                    query = query.Where(e => e.CategoryId == request.CategoryId.Value);
                }

                // Apply date range filter
                if (request.StartDate.HasValue)
                {
                    query = query.Where(e => e.EndDate >= request.StartDate.Value);
                }
                if (request.EndDate.HasValue)
                {
                    query = query.Where(e => e.StartDate <= request.EndDate.Value);
                }

                // Apply location filter
                if (!string.IsNullOrWhiteSpace(request.Location))
                {
                    var locationLower = request.Location.ToLower();
                    query = query.Where(e => e.Location.ToLower().Contains(locationLower));
                }

                // Apply location-based search (nearby events)
                if (request.Latitude.HasValue && request.Longitude.HasValue && request.RadiusKm.HasValue)
                {
                    // For simplicity, we'll filter in memory. In production, use spatial data types
                    var allEvents = await query.ToListAsync();
                    var nearbyEvents = allEvents.Where(e =>
                    {
                        if (!TryParseCoordinates(e.Location, out var lat, out var lng))
                            return false;

                        var distance = CalculateDistance(request.Latitude.Value, request.Longitude.Value, lat, lng);
                        return distance <= request.RadiusKm.Value;
                    }).ToList();

                    query = nearbyEvents.AsQueryable();
                }

                // Apply virtual/in-person filter
                if (request.IsVirtual.HasValue)
                {
                    query = query.Where(e => e.IsVirtual == request.IsVirtual.Value);
                }

                // Apply featured filter
                if (request.IsFeatured.HasValue)
                {
                    query = query.Where(e => e.IsFeatured == request.IsFeatured.Value);
                }

                // Apply organizer filter
                if (request.OrganizerId.HasValue)
                {
                    query = query.Where(e => e.OrganizerId == request.OrganizerId.Value);
                }

                // Apply tag filter
                if (request.Tags?.Any() == true)
                {
                    foreach (var tag in request.Tags)
                    {
                        query = query.Where(e => e.Tags.Contains(tag));
                    }
                }

                // Apply price range filter
                if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
                {
                    query = query.Where(e => e.TicketTypes.Any(tt =>
                        (!request.MinPrice.HasValue || tt.Price >= request.MinPrice.Value) &&
                        (!request.MaxPrice.HasValue || tt.Price <= request.MaxPrice.Value)));
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Calculate facets before applying sorting and pagination
                var facets = await CalculateFacetsAsync(query);

                // Apply sorting
                query = ApplySorting(query, request.SortBy, request.SortDirection);

                // Apply pagination
                var skip = (request.PageNumber - 1) * request.PageSize;
                var events = await query
                    .Skip(skip)
                    .Take(request.PageSize)
                    .ToListAsync();

                // Map to search results
                var results = events.Select(e => MapToSearchResult(e, request)).ToList();

                stopwatch.Stop();

                return new EventSearchResponse
                {
                    Results = results,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    Facets = facets,
                    SearchTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching events");
                throw;
            }
        }

        public async Task<SearchFacets> GetSearchFacetsAsync(EventSearchRequest request)
        {
            var query = _context.Events
                .Include(e => e.Category)
                .Include(e => e.TicketTypes)
                .Where(e => e.IsActive && e.Status == EventStatus.Published);

            // Apply the same filters as search but without pagination
            // ... (apply filters similar to SearchEventsAsync)

            return await CalculateFacetsAsync(query);
        }

        public async Task<PopularSearchTerms> GetPopularSearchTermsAsync(int count = 10)
        {
            // In a real implementation, this would query a search_logs table
            // For now, return mock data
            return new PopularSearchTerms
            {
                Terms = new List<SearchTermStat>
                {
                    new() { Term = "concert", SearchCount = 1523, TrendingScore = 0.95 },
                    new() { Term = "sports", SearchCount = 1234, TrendingScore = 0.85 },
                    new() { Term = "festival", SearchCount = 987, TrendingScore = 0.75 },
                    new() { Term = "conference", SearchCount = 876, TrendingScore = 0.65 },
                    new() { Term = "workshop", SearchCount = 654, TrendingScore = 0.55 }
                },
                LastUpdated = DateTime.UtcNow
            };
        }

        public async Task LogSearchTermAsync(string searchTerm, int? userId = null)
        {
            // In a real implementation, log to a search_logs table
            _logger.LogInformation("Search term logged: {SearchTerm} by user {UserId}", searchTerm, userId);
            await Task.CompletedTask;
        }

        private async Task<SearchFacets> CalculateFacetsAsync(IQueryable<Event> query)
        {
            var facets = new SearchFacets();

            // Category facets
            facets.Categories = await query
                .GroupBy(e => new { e.CategoryId, e.Category.Name, e.Category.Color })
                .Select(g => new CategoryFacet
                {
                    CategoryId = g.Key.CategoryId,
                    Name = g.Key.Name,
                    Color = g.Key.Color,
                    Count = g.Count()
                })
                .OrderByDescending(f => f.Count)
                .ToListAsync();

            // Price range facets
            var priceRanges = new[]
            {
                new { Label = "Free", Min = 0m, Max = 0m },
                new { Label = "$1 - $25", Min = 1m, Max = 25m },
                new { Label = "$26 - $50", Min = 26m, Max = 50m },
                new { Label = "$51 - $100", Min = 51m, Max = 100m },
                new { Label = "$101 - $250", Min = 101m, Max = 250m },
                new { Label = "$250+", Min = 251m, Max = decimal.MaxValue }
            };

            facets.PriceRanges = new List<PriceFacet>();
            foreach (var range in priceRanges)
            {
                var count = await query.CountAsync(e =>
                    e.TicketTypes.Any(tt => tt.Price >= range.Min && tt.Price <= range.Max));

                if (count > 0)
                {
                    facets.PriceRanges.Add(new PriceFacet
                    {
                        Label = range.Label,
                        MinPrice = range.Min,
                        MaxPrice = range.Max,
                        Count = count
                    });
                }
            }

            // Date range facets
            var now = DateTime.UtcNow;
            var dateRanges = new[]
            {
                new { Label = "Today", Start = now.Date, End = now.Date.AddDays(1) },
                new { Label = "Tomorrow", Start = now.Date.AddDays(1), End = now.Date.AddDays(2) },
                new { Label = "This Weekend", Start = GetNextWeekend(), End = GetNextWeekend().AddDays(2) },
                new { Label = "This Week", Start = now.Date, End = now.Date.AddDays(7) },
                new { Label = "Next Week", Start = now.Date.AddDays(7), End = now.Date.AddDays(14) },
                new { Label = "This Month", Start = new DateTime(now.Year, now.Month, 1), End = new DateTime(now.Year, now.Month, 1).AddMonths(1) }
            };

            facets.DateRanges = new List<DateFacet>();
            foreach (var range in dateRanges)
            {
                var count = await query.CountAsync(e =>
                    e.StartDate >= range.Start && e.StartDate < range.End);

                if (count > 0)
                {
                    facets.DateRanges.Add(new DateFacet
                    {
                        Label = range.Label,
                        StartDate = range.Start,
                        EndDate = range.End,
                        Count = count
                    });
                }
            }

            // Location facets (top 10 cities)
            var locationGroups = await query
                .Where(e => !string.IsNullOrEmpty(e.Location))
                .Select(e => e.Location)
                .ToListAsync();

            facets.Locations = locationGroups
                .GroupBy(l => ExtractCity(l))
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .Select(g => new LocationFacet
                {
                    City = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(f => f.Count)
                .Take(10)
                .ToList();

            // Popular tags
            var allTags = await query
                .Where(e => !string.IsNullOrEmpty(e.Tags))
                .Select(e => e.Tags)
                .ToListAsync();

            facets.PopularTags = allTags
                .SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(t => t.Trim())
                .GroupBy(t => t.ToLower())
                .Select(g => new TagFacet
                {
                    Tag = g.First(),
                    Count = g.Count()
                })
                .OrderByDescending(f => f.Count)
                .Take(20)
                .ToList();

            // Event type facets
            facets.EventTypes = new EventTypeFacets
            {
                VirtualCount = await query.CountAsync(e => e.IsVirtual),
                InPersonCount = await query.CountAsync(e => !e.IsVirtual && !string.IsNullOrEmpty(e.Location)),
                HybridCount = await query.CountAsync(e => e.IsVirtual && !string.IsNullOrEmpty(e.Location)),
                FeaturedCount = await query.CountAsync(e => e.IsFeatured)
            };

            return facets;
        }

        private IQueryable<Event> ApplySorting(IQueryable<Event> query, EventSortBy sortBy, SortDirection direction)
        {
            return sortBy switch
            {
                EventSortBy.StartDate => direction == SortDirection.Ascending
                    ? query.OrderBy(e => e.StartDate)
                    : query.OrderByDescending(e => e.StartDate),
                EventSortBy.EndDate => direction == SortDirection.Ascending
                    ? query.OrderBy(e => e.EndDate)
                    : query.OrderByDescending(e => e.EndDate),
                EventSortBy.Title => direction == SortDirection.Ascending
                    ? query.OrderBy(e => e.Title)
                    : query.OrderByDescending(e => e.Title),
                EventSortBy.Price => direction == SortDirection.Ascending
                    ? query.OrderBy(e => e.TicketTypes.Min(tt => tt.Price))
                    : query.OrderByDescending(e => e.TicketTypes.Max(tt => tt.Price)),
                EventSortBy.Popularity => direction == SortDirection.Ascending
                    ? query.OrderBy(e => e.Orders.Count())
                    : query.OrderByDescending(e => e.Orders.Count()),
                EventSortBy.CreatedDate => direction == SortDirection.Ascending
                    ? query.OrderBy(e => e.CreatedOn)
                    : query.OrderByDescending(e => e.CreatedOn),
                EventSortBy.UpdatedDate => direction == SortDirection.Ascending
                    ? query.OrderBy(e => e.UpdatedOn ?? e.CreatedOn)
                    : query.OrderByDescending(e => e.UpdatedOn ?? e.CreatedOn),
                _ => query.OrderBy(e => e.StartDate)
            };
        }

        private EventSearchResult MapToSearchResult(Event e, EventSearchRequest request)
        {
            var result = new EventSearchResult
            {
                EventId = e.EventId,
                Title = e.Title,
                Description = e.Description.Length > 200 
                    ? e.Description.Substring(0, 197) + "..." 
                    : e.Description,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Location = e.Location,
                VirtualUrl = e.VirtualUrl,
                ImageUrl = e.ImageUrl,
                Status = e.Status,
                CategoryId = e.CategoryId,
                CategoryName = e.Category?.Name ?? string.Empty,
                CategoryColor = e.Category?.Color ?? string.Empty,
                MaxCapacity = e.MaxCapacity,
                CurrentCapacity = e.CurrentCapacity,
                MinPrice = e.TicketTypes.Any() ? e.TicketTypes.Min(tt => tt.Price) : 0,
                MaxPrice = e.TicketTypes.Any() ? e.TicketTypes.Max(tt => tt.Price) : 0,
                OrganizerName = $"{e.Organizer?.FirstName} {e.Organizer?.LastName}".Trim(),
                CustomUrl = e.CustomUrl,
                Tags = string.IsNullOrEmpty(e.Tags) 
                    ? new List<string>() 
                    : e.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList(),
                IsVirtual = e.IsVirtual,
                IsFeatured = e.IsFeatured,
                TotalTicketsSold = e.Orders?.Sum(o => o.OrderItems.Sum(oi => oi.Quantity)) ?? 0
            };

            // Calculate distance if location search
            if (request.Latitude.HasValue && request.Longitude.HasValue && 
                TryParseCoordinates(e.Location, out var lat, out var lng))
            {
                result.Distance = CalculateDistance(request.Latitude.Value, request.Longitude.Value, lat, lng);
            }

            // Calculate relevance score for text search
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                result.Relevance = CalculateRelevance(e, request.SearchTerm);
            }

            return result;
        }

        private double CalculateRelevance(Event e, string searchTerm)
        {
            var termLower = searchTerm.ToLower();
            double score = 0;

            // Title match (highest weight)
            if (e.Title.ToLower().Contains(termLower))
                score += 10;
            if (e.Title.ToLower().StartsWith(termLower))
                score += 5;

            // Description match
            if (e.Description.ToLower().Contains(termLower))
                score += 3;

            // Location match
            if (e.Location.ToLower().Contains(termLower))
                score += 2;

            // Tag match
            if (e.Tags.ToLower().Contains(termLower))
                score += 1;

            return score;
        }

        private bool TryParseCoordinates(string location, out double latitude, out double longitude)
        {
            // Simple implementation - in production, use geocoding service
            latitude = 0;
            longitude = 0;
            return false;
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula for calculating distance between two points
            const double R = 6371; // Earth's radius in kilometers
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        private DateTime GetNextWeekend()
        {
            var today = DateTime.UtcNow.Date;
            var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7;
            return today.AddDays(daysUntilSaturday);
        }

        private string ExtractCity(string location)
        {
            // Simple implementation - extract first part before comma
            if (string.IsNullOrEmpty(location))
                return string.Empty;

            var parts = location.Split(',');
            return parts[0].Trim();
        }
    }
}