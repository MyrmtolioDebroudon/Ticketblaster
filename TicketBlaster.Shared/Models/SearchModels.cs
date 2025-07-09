using System.ComponentModel.DataAnnotations;

namespace TicketBlaster.Shared.Models
{
    public class EventSearchRequest
    {
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public List<int>? CategoryIds { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Location { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public List<string>? Tags { get; set; }
        public bool? IsVirtual { get; set; }
        public bool? IsFeatured { get; set; }
        public EventStatus? Status { get; set; }
        public int? OrganizerId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? RadiusKm { get; set; }
        public EventSortBy SortBy { get; set; } = EventSortBy.StartDate;
        public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class EventSearchResponse
    {
        public List<EventSearchResult> Results { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public SearchFacets Facets { get; set; } = new();
        public long SearchTimeMs { get; set; }
    }

    public class EventSearchResult
    {
        public int EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string VirtualUrl { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public EventStatus Status { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
        public int MaxCapacity { get; set; }
        public int CurrentCapacity { get; set; }
        public int AvailableCapacity => MaxCapacity - CurrentCapacity;
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public string OrganizerName { get; set; } = string.Empty;
        public string CustomUrl { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public bool IsVirtual { get; set; }
        public bool IsFeatured { get; set; }
        public double? Distance { get; set; }
        public double? Relevance { get; set; }
        public int TotalTicketsSold { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }

    public class SearchFacets
    {
        public List<CategoryFacet> Categories { get; set; } = new();
        public List<PriceFacet> PriceRanges { get; set; } = new();
        public List<DateFacet> DateRanges { get; set; } = new();
        public List<LocationFacet> Locations { get; set; } = new();
        public List<TagFacet> PopularTags { get; set; } = new();
        public EventTypeFacets EventTypes { get; set; } = new();
    }

    public class CategoryFacet
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class PriceFacet
    {
        public string Label { get; set; } = string.Empty;
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public int Count { get; set; }
    }

    public class DateFacet
    {
        public string Label { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Count { get; set; }
    }

    public class LocationFacet
    {
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TagFacet
    {
        public string Tag { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class EventTypeFacets
    {
        public int VirtualCount { get; set; }
        public int InPersonCount { get; set; }
        public int HybridCount { get; set; }
        public int FeaturedCount { get; set; }
    }

    public enum EventSortBy
    {
        StartDate,
        EndDate,
        Title,
        Price,
        Popularity,
        Distance,
        Relevance,
        CreatedDate,
        UpdatedDate
    }

    public enum SortDirection
    {
        Ascending,
        Descending
    }

    public class EventDiscoveryRequest
    {
        public int? UserId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public List<int>? PreferredCategories { get; set; }
        public int Count { get; set; } = 10;
    }

    public class EventDiscoveryResponse
    {
        public List<DiscoverySection> Sections { get; set; } = new();
    }

    public class DiscoverySection
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DiscoverySectionType Type { get; set; }
        public List<EventSearchResult> Events { get; set; } = new();
        public string ViewMoreUrl { get; set; } = string.Empty;
    }

    public enum DiscoverySectionType
    {
        Trending,
        NearYou,
        Recommended,
        Featured,
        UpcomingThisWeek,
        PopularCategories,
        NewlyAdded,
        LastChance,
        FreeEvents,
        VirtualEvents
    }

    public class EventRecommendationRequest
    {
        public int UserId { get; set; }
        public int? EventId { get; set; }
        public int Count { get; set; } = 10;
        public RecommendationType Type { get; set; } = RecommendationType.UserBased;
    }

    public enum RecommendationType
    {
        UserBased,
        EventBased,
        CategoryBased,
        CollaborativeFiltering,
        ContentBased,
        Hybrid
    }

    public class TrendingEventsRequest
    {
        public int? CategoryId { get; set; }
        public TrendingPeriod Period { get; set; } = TrendingPeriod.Last7Days;
        public int Count { get; set; } = 10;
    }

    public enum TrendingPeriod
    {
        Last24Hours,
        Last3Days,
        Last7Days,
        Last30Days
    }

    public class PopularSearchTerms
    {
        public List<SearchTermStat> Terms { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public class SearchTermStat
    {
        public string Term { get; set; } = string.Empty;
        public int SearchCount { get; set; }
        public double TrendingScore { get; set; }
    }

    public class EventStatisticsResponse
    {
        public int EventId { get; set; }
        public int ViewCount { get; set; }
        public int SaveCount { get; set; }
        public int ShareCount { get; set; }
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
        public double ConversionRate { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public Dictionary<DateTime, int> DailyViews { get; set; } = new();
        public Dictionary<DateTime, int> DailyTicketSales { get; set; } = new();
    }
}