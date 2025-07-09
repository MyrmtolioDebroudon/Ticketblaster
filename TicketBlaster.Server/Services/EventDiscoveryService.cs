using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TicketBlaster.Database;
using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public interface IEventDiscoveryService
    {
        Task<EventDiscoveryResponse> GetDiscoveryContentAsync(EventDiscoveryRequest request);
        Task<List<EventSearchResult>> GetTrendingEventsAsync(TrendingEventsRequest request);
        Task<List<EventSearchResult>> GetRecommendedEventsAsync(EventRecommendationRequest request);
        Task<List<EventSearchResult>> GetSimilarEventsAsync(int eventId, int count = 5);
        Task<List<EventSearchResult>> GetEventsNearUserAsync(double latitude, double longitude, double radiusKm = 50, int count = 10);
        Task<EventStatisticsResponse> GetEventStatisticsAsync(int eventId);
        Task RecordEventViewAsync(int eventId, int? userId = null);
        Task RecordEventInteractionAsync(int eventId, int userId, string interactionType);
    }

    public class EventDiscoveryService : IEventDiscoveryService
    {
        private readonly TicketBlasterDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<EventDiscoveryService> _logger;

        public EventDiscoveryService(
            TicketBlasterDbContext context,
            IMemoryCache cache,
            ILogger<EventDiscoveryService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<EventDiscoveryResponse> GetDiscoveryContentAsync(EventDiscoveryRequest request)
        {
            var response = new EventDiscoveryResponse();
            var sections = new List<DiscoverySection>();

            // Featured Events
            var featuredEvents = await GetFeaturedEventsAsync();
            if (featuredEvents.Any())
            {
                sections.Add(new DiscoverySection
                {
                    Title = "Featured Events",
                    Description = "Hand-picked events you shouldn't miss",
                    Type = DiscoverySectionType.Featured,
                    Events = featuredEvents,
                    ViewMoreUrl = "/events/featured"
                });
            }

            // Trending Events
            var trendingEvents = await GetTrendingEventsAsync(new TrendingEventsRequest { Count = 10 });
            if (trendingEvents.Any())
            {
                sections.Add(new DiscoverySection
                {
                    Title = "Trending Now",
                    Description = "Popular events everyone's talking about",
                    Type = DiscoverySectionType.Trending,
                    Events = trendingEvents,
                    ViewMoreUrl = "/events/trending"
                });
            }

            // Events Near User
            if (request.Latitude.HasValue && request.Longitude.HasValue)
            {
                var nearbyEvents = await GetEventsNearUserAsync(
                    request.Latitude.Value,
                    request.Longitude.Value,
                    50, // 50km radius
                    10);

                if (nearbyEvents.Any())
                {
                    sections.Add(new DiscoverySection
                    {
                        Title = "Near You",
                        Description = "Events happening in your area",
                        Type = DiscoverySectionType.NearYou,
                        Events = nearbyEvents,
                        ViewMoreUrl = "/events/nearby"
                    });
                }
            }

            // Upcoming This Week
            var thisWeekEvents = await GetUpcomingEventsThisWeekAsync();
            if (thisWeekEvents.Any())
            {
                sections.Add(new DiscoverySection
                {
                    Title = "This Week",
                    Description = "Don't miss these upcoming events",
                    Type = DiscoverySectionType.UpcomingThisWeek,
                    Events = thisWeekEvents,
                    ViewMoreUrl = "/events/this-week"
                });
            }

            // Virtual Events
            var virtualEvents = await GetVirtualEventsAsync();
            if (virtualEvents.Any())
            {
                sections.Add(new DiscoverySection
                {
                    Title = "Virtual Events",
                    Description = "Join from anywhere in the world",
                    Type = DiscoverySectionType.VirtualEvents,
                    Events = virtualEvents,
                    ViewMoreUrl = "/events/virtual"
                });
            }

            // Free Events
            var freeEvents = await GetFreeEventsAsync();
            if (freeEvents.Any())
            {
                sections.Add(new DiscoverySection
                {
                    Title = "Free Events",
                    Description = "Great experiences at no cost",
                    Type = DiscoverySectionType.FreeEvents,
                    Events = freeEvents,
                    ViewMoreUrl = "/events/free"
                });
            }

            // Personalized Recommendations
            if (request.UserId.HasValue)
            {
                var recommendations = await GetRecommendedEventsAsync(new EventRecommendationRequest
                {
                    UserId = request.UserId.Value,
                    Count = 10
                });

                if (recommendations.Any())
                {
                    sections.Add(new DiscoverySection
                    {
                        Title = "Recommended for You",
                        Description = "Based on your interests and past events",
                        Type = DiscoverySectionType.Recommended,
                        Events = recommendations,
                        ViewMoreUrl = "/events/recommended"
                    });
                }
            }

            // Newly Added
            var newEvents = await GetNewlyAddedEventsAsync();
            if (newEvents.Any())
            {
                sections.Add(new DiscoverySection
                {
                    Title = "Just Added",
                    Description = "Fresh events recently posted",
                    Type = DiscoverySectionType.NewlyAdded,
                    Events = newEvents,
                    ViewMoreUrl = "/events/new"
                });
            }

            // Last Chance
            var lastChanceEvents = await GetLastChanceEventsAsync();
            if (lastChanceEvents.Any())
            {
                sections.Add(new DiscoverySection
                {
                    Title = "Last Chance",
                    Description = "Limited tickets remaining",
                    Type = DiscoverySectionType.LastChance,
                    Events = lastChanceEvents,
                    ViewMoreUrl = "/events/last-chance"
                });
            }

            response.Sections = sections;
            return response;
        }

        public async Task<List<EventSearchResult>> GetTrendingEventsAsync(TrendingEventsRequest request)
        {
            var cacheKey = $"trending_events_{request.CategoryId}_{request.Period}_{request.Count}";
            
            if (_cache.TryGetValue<List<EventSearchResult>>(cacheKey, out var cachedEvents))
            {
                return cachedEvents;
            }

            var startDate = GetStartDateForPeriod(request.Period);
            
            var query = _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .Include(e => e.Orders)
                .Where(e => e.IsActive && e.Status == EventStatus.Published);

            if (request.CategoryId.HasValue)
            {
                query = query.Where(e => e.CategoryId == request.CategoryId.Value);
            }

            // Calculate trending score based on recent activity
            var trendingEvents = await query
                .Where(e => e.StartDate >= DateTime.UtcNow)
                .Select(e => new
                {
                    Event = e,
                    RecentOrders = e.Orders.Count(o => o.CreatedOn >= startDate),
                    TotalOrders = e.Orders.Count(),
                    DaysUntilEvent = (e.StartDate - DateTime.UtcNow).TotalDays
                })
                .ToListAsync();

            var results = trendingEvents
                .Select(te => new
                {
                    te.Event,
                    TrendingScore = CalculateTrendingScore(
                        te.RecentOrders,
                        te.TotalOrders,
                        te.DaysUntilEvent,
                        te.Event.IsFeatured)
                })
                .OrderByDescending(te => te.TrendingScore)
                .Take(request.Count)
                .Select(te => MapToSearchResult(te.Event))
                .ToList();

            _cache.Set(cacheKey, results, TimeSpan.FromMinutes(15));
            return results;
        }

        public async Task<List<EventSearchResult>> GetRecommendedEventsAsync(EventRecommendationRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Orders)
                    .ThenInclude(o => o.Event)
                        .ThenInclude(e => e.Category)
                .FirstOrDefaultAsync(u => u.UserId == request.UserId);

            if (user == null)
            {
                return new List<EventSearchResult>();
            }

            // Get user's preferred categories based on past orders
            var preferredCategories = user.Orders
                .Where(o => o.Status == OrderStatus.Completed)
                .Select(o => o.Event.CategoryId)
                .GroupBy(c => c)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(3)
                .ToList();

            // Get attended event tags
            var attendedEventTags = user.Orders
                .Where(o => o.Status == OrderStatus.Completed)
                .SelectMany(o => (o.Event.Tags ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(t => t.Trim().ToLower())
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(10)
                .ToList();

            // Build recommendation query
            var query = _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .Where(e => e.IsActive && 
                           e.Status == EventStatus.Published &&
                           e.StartDate >= DateTime.UtcNow);

            // Exclude events user already has tickets for
            var userEventIds = user.Orders.Select(o => o.EventId).ToList();
            query = query.Where(e => !userEventIds.Contains(e.EventId));

            // Score events based on user preferences
            var scoredEvents = await query.ToListAsync();
            
            var recommendations = scoredEvents
                .Select(e => new
                {
                    Event = e,
                    Score = CalculateRecommendationScore(e, preferredCategories, attendedEventTags)
                })
                .OrderByDescending(se => se.Score)
                .Take(request.Count)
                .Select(se => MapToSearchResult(se.Event))
                .ToList();

            return recommendations;
        }

        public async Task<List<EventSearchResult>> GetSimilarEventsAsync(int eventId, int count = 5)
        {
            var targetEvent = await _context.Events
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (targetEvent == null)
            {
                return new List<EventSearchResult>();
            }

            var targetTags = (targetEvent.Tags ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLower())
                .ToList();

            var query = _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .Where(e => e.IsActive && 
                           e.Status == EventStatus.Published &&
                           e.EventId != eventId &&
                           e.StartDate >= DateTime.UtcNow);

            var allEvents = await query.ToListAsync();

            var similarEvents = allEvents
                .Select(e => new
                {
                    Event = e,
                    SimilarityScore = CalculateSimilarityScore(e, targetEvent, targetTags)
                })
                .OrderByDescending(se => se.SimilarityScore)
                .Take(count)
                .Select(se => MapToSearchResult(se.Event))
                .ToList();

            return similarEvents;
        }

        public async Task<List<EventSearchResult>> GetEventsNearUserAsync(double latitude, double longitude, double radiusKm = 50, int count = 10)
        {
            // In production, use spatial queries with SQL Server spatial types
            var events = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .Where(e => e.IsActive && 
                           e.Status == EventStatus.Published &&
                           e.StartDate >= DateTime.UtcNow &&
                           !e.IsVirtual &&
                           !string.IsNullOrEmpty(e.Location))
                .ToListAsync();

            var nearbyEvents = events
                .Select(e => new
                {
                    Event = e,
                    Distance = CalculateDistanceFromLocation(e.Location, latitude, longitude)
                })
                .Where(ne => ne.Distance.HasValue && ne.Distance.Value <= radiusKm)
                .OrderBy(ne => ne.Distance)
                .Take(count)
                .Select(ne => 
                {
                    var result = MapToSearchResult(ne.Event);
                    result.Distance = ne.Distance;
                    return result;
                })
                .ToList();

            return nearbyEvents;
        }

        public async Task<EventStatisticsResponse> GetEventStatisticsAsync(int eventId)
        {
            var eventStats = await _context.Events
                .Include(e => e.Orders)
                    .ThenInclude(o => o.OrderItems)
                .Where(e => e.EventId == eventId)
                .Select(e => new EventStatisticsResponse
                {
                    EventId = e.EventId,
                    TicketsSold = e.Orders
                        .Where(o => o.Status == OrderStatus.Completed)
                        .Sum(o => o.OrderItems.Sum(oi => oi.Quantity)),
                    Revenue = e.Orders
                        .Where(o => o.Status == OrderStatus.Completed)
                        .Sum(o => o.TotalAmount),
                    // View count would come from a separate analytics table
                    ViewCount = 0,
                    SaveCount = 0,
                    ShareCount = 0
                })
                .FirstOrDefaultAsync();

            if (eventStats != null)
            {
                // Calculate conversion rate
                eventStats.ConversionRate = eventStats.ViewCount > 0 
                    ? (double)eventStats.TicketsSold / eventStats.ViewCount * 100 
                    : 0;

                // Get daily statistics (mock data for now)
                eventStats.DailyViews = GetMockDailyStats(eventId, 30);
                eventStats.DailyTicketSales = GetMockDailyStats(eventId, 30);
            }

            return eventStats ?? new EventStatisticsResponse { EventId = eventId };
        }

        public async Task RecordEventViewAsync(int eventId, int? userId = null)
        {
            // In production, record to an analytics table
            _logger.LogInformation("Event view recorded: EventId={EventId}, UserId={UserId}", eventId, userId);
            await Task.CompletedTask;
        }

        public async Task RecordEventInteractionAsync(int eventId, int userId, string interactionType)
        {
            // In production, record to an interactions table
            _logger.LogInformation("Event interaction recorded: EventId={EventId}, UserId={UserId}, Type={Type}", 
                eventId, userId, interactionType);
            await Task.CompletedTask;
        }

        private async Task<List<EventSearchResult>> GetFeaturedEventsAsync()
        {
            var events = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .Where(e => e.IsActive && 
                           e.Status == EventStatus.Published &&
                           e.IsFeatured &&
                           e.StartDate >= DateTime.UtcNow)
                .OrderBy(e => e.StartDate)
                .Take(10)
                .ToListAsync();

            return events.Select(MapToSearchResult).ToList();
        }

        private async Task<List<EventSearchResult>> GetUpcomingEventsThisWeekAsync()
        {
            var endOfWeek = DateTime.UtcNow.Date.AddDays(7);

            var events = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .Where(e => e.IsActive && 
                           e.Status == EventStatus.Published &&
                           e.StartDate >= DateTime.UtcNow &&
                           e.StartDate <= endOfWeek)
                .OrderBy(e => e.StartDate)
                .Take(10)
                .ToListAsync();

            return events.Select(MapToSearchResult).ToList();
        }

        private async Task<List<EventSearchResult>> GetVirtualEventsAsync()
        {
            var events = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .Where(e => e.IsActive && 
                           e.Status == EventStatus.Published &&
                           e.IsVirtual &&
                           e.StartDate >= DateTime.UtcNow)
                .OrderBy(e => e.StartDate)
                .Take(10)
                .ToListAsync();

            return events.Select(MapToSearchResult).ToList();
        }

        private async Task<List<EventSearchResult>> GetFreeEventsAsync()
        {
            var events = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .Where(e => e.IsActive && 
                           e.Status == EventStatus.Published &&
                           e.StartDate >= DateTime.UtcNow &&
                           e.TicketTypes.Any(tt => tt.Price == 0))
                .OrderBy(e => e.StartDate)
                .Take(10)
                .ToListAsync();

            return events.Select(MapToSearchResult).ToList();
        }

        private async Task<List<EventSearchResult>> GetNewlyAddedEventsAsync()
        {
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            var events = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .Where(e => e.IsActive && 
                           e.Status == EventStatus.Published &&
                           e.CreatedOn >= sevenDaysAgo &&
                           e.StartDate >= DateTime.UtcNow)
                .OrderByDescending(e => e.CreatedOn)
                .Take(10)
                .ToListAsync();

            return events.Select(MapToSearchResult).ToList();
        }

        private async Task<List<EventSearchResult>> GetLastChanceEventsAsync()
        {
            var events = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .Where(e => e.IsActive && 
                           e.Status == EventStatus.Published &&
                           e.StartDate >= DateTime.UtcNow)
                .ToListAsync();

            // Filter for events with limited availability
            var lastChanceEvents = events
                .Where(e => 
                {
                    var availableTickets = e.TicketTypes.Sum(tt => tt.AvailableQuantity);
                    var totalTickets = e.TicketTypes.Sum(tt => tt.Quantity);
                    return totalTickets > 0 && (double)availableTickets / totalTickets < 0.2; // Less than 20% remaining
                })
                .OrderBy(e => e.StartDate)
                .Take(10)
                .Select(MapToSearchResult)
                .ToList();

            return lastChanceEvents;
        }

        private EventSearchResult MapToSearchResult(Event e)
        {
            return new EventSearchResult
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
        }

        private DateTime GetStartDateForPeriod(TrendingPeriod period)
        {
            return period switch
            {
                TrendingPeriod.Last24Hours => DateTime.UtcNow.AddHours(-24),
                TrendingPeriod.Last3Days => DateTime.UtcNow.AddDays(-3),
                TrendingPeriod.Last7Days => DateTime.UtcNow.AddDays(-7),
                TrendingPeriod.Last30Days => DateTime.UtcNow.AddDays(-30),
                _ => DateTime.UtcNow.AddDays(-7)
            };
        }

        private double CalculateTrendingScore(int recentOrders, int totalOrders, double daysUntilEvent, bool isFeatured)
        {
            // Calculate trending score based on multiple factors
            double score = 0;

            // Recent activity (weight: 40%)
            score += recentOrders * 0.4;

            // Total popularity (weight: 20%)
            score += Math.Min(totalOrders * 0.01, 20);

            // Urgency - events happening soon get boost (weight: 20%)
            if (daysUntilEvent <= 7)
                score += (7 - daysUntilEvent) * 2;

            // Featured events get boost (weight: 20%)
            if (isFeatured)
                score += 20;

            return score;
        }

        private double CalculateRecommendationScore(Event e, List<int> preferredCategories, List<string> preferredTags)
        {
            double score = 0;

            // Category match (weight: 40%)
            if (preferredCategories.Contains(e.CategoryId))
            {
                var categoryIndex = preferredCategories.IndexOf(e.CategoryId);
                score += (3 - categoryIndex) * 13.33; // Higher score for more preferred categories
            }

            // Tag match (weight: 30%)
            if (!string.IsNullOrEmpty(e.Tags))
            {
                var eventTags = e.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim().ToLower())
                    .ToList();

                var matchingTags = eventTags.Intersect(preferredTags).Count();
                score += matchingTags * 10;
            }

            // Popularity (weight: 20%)
            var ticketsSold = e.Orders?.Sum(o => o.OrderItems.Sum(oi => oi.Quantity)) ?? 0;
            score += Math.Min(ticketsSold * 0.1, 20);

            // Featured events (weight: 10%)
            if (e.IsFeatured)
                score += 10;

            return score;
        }

        private double CalculateSimilarityScore(Event e, Event targetEvent, List<string> targetTags)
        {
            double score = 0;

            // Same category (weight: 40%)
            if (e.CategoryId == targetEvent.CategoryId)
                score += 40;

            // Tag similarity (weight: 30%)
            if (!string.IsNullOrEmpty(e.Tags))
            {
                var eventTags = e.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim().ToLower())
                    .ToList();

                var commonTags = eventTags.Intersect(targetTags).Count();
                var totalTags = eventTags.Union(targetTags).Count();
                
                if (totalTags > 0)
                {
                    var similarity = (double)commonTags / totalTags;
                    score += similarity * 30;
                }
            }

            // Same organizer (weight: 10%)
            if (e.OrganizerId == targetEvent.OrganizerId)
                score += 10;

            // Price range similarity (weight: 10%)
            if (e.TicketTypes.Any() && targetEvent.TicketTypes.Any())
            {
                var avgPrice = e.TicketTypes.Average(tt => tt.Price);
                var targetAvgPrice = targetEvent.TicketTypes.Average(tt => tt.Price);
                var priceDiff = Math.Abs(avgPrice - targetAvgPrice);
                
                if (targetAvgPrice > 0)
                {
                    var priceRatio = 1 - Math.Min(priceDiff / targetAvgPrice, 1);
                    score += priceRatio * 10;
                }
            }

            // Event type similarity (weight: 10%)
            if (e.IsVirtual == targetEvent.IsVirtual)
                score += 10;

            return score;
        }

        private double? CalculateDistanceFromLocation(string location, double targetLat, double targetLng)
        {
            // In production, use geocoding service or store coordinates
            // For now, return mock distance
            return new Random().NextDouble() * 100;
        }

        private Dictionary<DateTime, int> GetMockDailyStats(int eventId, int days)
        {
            var stats = new Dictionary<DateTime, int>();
            var random = new Random(eventId);

            for (int i = days - 1; i >= 0; i--)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i);
                stats[date] = random.Next(50, 500);
            }

            return stats;
        }
    }
}