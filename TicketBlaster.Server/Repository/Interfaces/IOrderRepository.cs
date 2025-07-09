using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Repository.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order?> GetOrderAsync(int orderId);
        Task<Order?> GetOrderByNumberAsync(string orderNumber);
        Task<IEnumerable<Order>> GetUserOrdersAsync(int userId);
        Task<IEnumerable<Order>> GetEventOrdersAsync(int eventId);
        Task<Order> CreateOrderAsync(Order order);
        Task<Order> UpdateOrderAsync(Order order);
        Task<bool> DeleteOrderAsync(int orderId);
        Task<IEnumerable<OrderItem>> GetOrderItemsAsync(int orderId);
        Task<OrderItem> CreateOrderItemAsync(OrderItem orderItem);
        Task<IEnumerable<Ticket>> GenerateTicketsAsync(int orderItemId, int quantity);
    }
}