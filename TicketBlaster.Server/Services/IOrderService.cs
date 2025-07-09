using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public interface IOrderService
    {
        Task<Order?> GetOrderAsync(int orderId);
        Task<Order?> GetOrderByNumberAsync(string orderNumber);
        Task<IEnumerable<Order>> GetUserOrdersAsync(int userId);
        Task<IEnumerable<Order>> GetEventOrdersAsync(int eventId);
        Task<Order> CreateOrderAsync(CreateOrderRequest request);
        Task<Order> UpdateOrderAsync(Order order);
        Task<bool> CancelOrderAsync(int orderId);
        Task<bool> ProcessPaymentAsync(int orderId, string paymentIntentId);
        Task<bool> RefundOrderAsync(int orderId, decimal? refundAmount = null);
        Task<OrderSummary> CalculateOrderSummaryAsync(List<OrderItemRequest> items);
        Task<string> GenerateOrderNumberAsync();
        Task<bool> ValidateOrderAsync(int orderId);
    }

    public class CreateOrderRequest
    {
        public int EventId { get; set; }
        public int UserId { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new();
        public CustomerInfo Customer { get; set; } = new();
        public BillingAddress Billing { get; set; } = new();
        public string? Notes { get; set; }
    }

    public class OrderItemRequest
    {
        public int TicketTypeId { get; set; }
        public int Quantity { get; set; }
        public string? DiscountCode { get; set; }
    }

    public class CustomerInfo
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    public class BillingAddress
    {
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    public class OrderSummary
    {
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderItemSummary> Items { get; set; } = new();
    }

    public class OrderItemSummary
    {
        public string TicketTypeName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }
        public string? DiscountCode { get; set; }
    }
}