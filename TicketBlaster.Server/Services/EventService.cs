using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public class EventService : IEventService
    {
        public Task<Event?> GetEventAsync(int eventId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Event>> GetEventsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Event> CreateEventAsync(Event eventItem)
        {
            throw new NotImplementedException();
        }

        public Task<Event> UpdateEventAsync(Event eventItem)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteEventAsync(int eventId)
        {
            throw new NotImplementedException();
        }
    }
}