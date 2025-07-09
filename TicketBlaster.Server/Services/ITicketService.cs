using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public interface ITicketService
    {
        Task<TicketType?> GetTicketTypeAsync(int ticketTypeId);
        Task<IEnumerable<TicketType>> GetEventTicketTypesAsync(int eventId);
        Task<TicketType> CreateTicketTypeAsync(TicketType ticketType);
        Task<TicketType> UpdateTicketTypeAsync(TicketType ticketType);
        Task<bool> DeleteTicketTypeAsync(int ticketTypeId);
        Task<bool> CheckTicketAvailabilityAsync(int ticketTypeId, int quantity);
        Task<bool> ReserveTicketsAsync(int ticketTypeId, int quantity, TimeSpan reservationDuration);
        Task<bool> ReleaseTicketReservationAsync(int ticketTypeId, int quantity);
        Task<IEnumerable<Ticket>> GetUserTicketsAsync(int userId);
        Task<Ticket?> GetTicketByNumberAsync(string ticketNumber);
        Task<bool> CheckInTicketAsync(string ticketNumber);
        Task<bool> TransferTicketAsync(int ticketId, int newOwnerId);
        Task<TicketTypeDiscount?> ValidateDiscountCodeAsync(string discountCode, int ticketTypeId);
        Task<decimal> CalculateTicketPriceAsync(int ticketTypeId, int quantity, string? discountCode = null);
    }
}