using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public class TicketService : ITicketService
    {
        public Task<Ticket?> GetTicketAsync(int ticketId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Ticket>> GetOrderTicketsAsync(int orderId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ValidateTicketAsync(string ticketNumber)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CheckInTicketAsync(string ticketNumber)
        {
            throw new NotImplementedException();
        }
    }
}