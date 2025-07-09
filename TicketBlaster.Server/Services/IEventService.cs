using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public interface IEventService
    {
        Task<Event?> GetEventAsync(int eventId);
        Task<Event?> GetEventByCustomUrlAsync(string customUrl);
        Task<IEnumerable<Event>> GetEventsAsync(int organizerId);
        Task<IEnumerable<Event>> GetPublicEventsAsync(int pageNumber = 1, int pageSize = 20);
        Task<IEnumerable<Event>> SearchEventsAsync(string searchTerm, int? categoryId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<Event> CreateEventAsync(Event eventEntity);
        Task<Event> UpdateEventAsync(Event eventEntity);
        Task<bool> DeleteEventAsync(int eventId);
        Task<bool> PublishEventAsync(int eventId);
        Task<bool> CancelEventAsync(int eventId);
        Task<IEnumerable<Event>> GetFeaturedEventsAsync(int count = 10);
        Task<IEnumerable<Event>> GetEventsByCategoryAsync(int categoryId, int pageNumber = 1, int pageSize = 20);
        Task<bool> IsEventOrganizerAsync(int eventId, int userId);
        Task<EventStatistics> GetEventStatisticsAsync(int eventId);
    }

    public class EventStatistics
    {
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalViews { get; set; }
        public int TotalOrders { get; set; }
        public Dictionary<string, int> TicketSalesByType { get; set; } = new();
        public Dictionary<DateTime, int> DailySales { get; set; } = new();
    }
}