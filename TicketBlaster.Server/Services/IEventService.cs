using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public interface IEventService
    {
        Task<Event?> GetEventAsync(int eventId);
        Task<IEnumerable<Event>> GetEventsAsync();
        Task<Event> CreateEventAsync(Event eventItem);
        Task<Event> UpdateEventAsync(Event eventItem);
        Task<bool> DeleteEventAsync(int eventId);
    }
}