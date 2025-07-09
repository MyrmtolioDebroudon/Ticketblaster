using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public interface ITicketService
    {
        Task<Ticket?> GetTicketAsync(int ticketId);
        Task<IEnumerable<Ticket>> GetOrderTicketsAsync(int orderId);
        Task<bool> ValidateTicketAsync(string ticketNumber);
        Task<bool> CheckInTicketAsync(string ticketNumber);
    }
}