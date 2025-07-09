using TicketBlaster.Shared.Models;
using TicketBlaster.Server.Repository.Interfaces;
using TicketBlaster.Server.Infrastructure;
using Microsoft.AspNetCore.SignalR;

namespace TicketBlaster.Server.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IHubContext<OrderStatusHub> _orderStatusHub;
        private readonly IHubContext<TicketInventoryHub> _ticketInventoryHub;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository orderRepository,
            IHubContext<OrderStatusHub> orderStatusHub,
            IHubContext<TicketInventoryHub> ticketInventoryHub,
            ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _orderStatusHub = orderStatusHub;
            _ticketInventoryHub = ticketInventoryHub;
            _logger = logger;
        }

        public async Task<Order?> GetOrderAsync(int orderId)
        {
            return await _orderRepository.GetOrderAsync(orderId);
        }

        public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
        {
            return await _orderRepository.GetOrderByNumberAsync(orderNumber);
        }

        public async Task<IEnumerable<Order>> GetUserOrdersAsync(int userId)
        {
            return await _orderRepository.GetUserOrdersAsync(userId);
        }

        public async Task<IEnumerable<Order>> GetEventOrdersAsync(int eventId)
        {
            return await _orderRepository.GetEventOrdersAsync(eventId);
        }

        public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
        {
            try
            {
                // Calculate order summary
                var summary = await CalculateOrderSummaryAsync(request.Items);

                // Create order
                var order = new Order
                {
                    EventId = request.EventId,
                    UserId = request.UserId,
                    OrderNumber = await GenerateOrderNumberAsync(),
                    SubTotal = summary.SubTotal,
                    TaxAmount = summary.TaxAmount,
                    ServiceFee = summary.ServiceFee,
                    DiscountAmount = summary.DiscountAmount,
                    TotalAmount = summary.TotalAmount,
                    Currency = "USD",
                    Status = OrderStatus.Pending,
                    CustomerFirstName = request.Customer.FirstName,
                    CustomerLastName = request.Customer.LastName,
                    CustomerEmail = request.Customer.Email,
                    CustomerPhone = request.Customer.Phone,
                    BillingAddress = request.Billing.Address,
                    BillingCity = request.Billing.City,
                    BillingState = request.Billing.State,
                    BillingZipCode = request.Billing.ZipCode,
                    BillingCountry = request.Billing.Country,
                    Notes = request.Notes ?? string.Empty,
                    OrderDate = DateTime.UtcNow,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = request.UserId
                };

                order = await _orderRepository.CreateOrderAsync(order);

                // Create order items
                foreach (var item in request.Items)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,
                        TicketTypeId = item.TicketTypeId,
                        Quantity = item.Quantity,
                        UnitPrice = summary.ItemPrices[item.TicketTypeId],
                        DiscountAmount = 0, // TODO: Apply discount codes
                        TotalPrice = summary.ItemPrices[item.TicketTypeId] * item.Quantity,
                        DiscountCode = item.DiscountCode ?? string.Empty,
                        CreatedOn = DateTime.UtcNow
                    };

                    await _orderRepository.CreateOrderItemAsync(orderItem);

                    // Generate tickets
                    await _orderRepository.GenerateTicketsAsync(orderItem.OrderItemId, orderItem.Quantity);
                }

                // Notify via SignalR
                await _orderStatusHub.Clients.User(request.UserId.ToString())
                    .SendAsync("OrderCreated", order.OrderId, order.OrderNumber);

                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                throw;
            }
        }

        public async Task<Order> UpdateOrderAsync(Order order)
        {
            var updatedOrder = await _orderRepository.UpdateOrderAsync(order);
            
            // Notify via SignalR
            await _orderStatusHub.Clients.User(order.UserId.ToString())
                .SendAsync("OrderUpdated", order.OrderId, order.Status.ToString());

            return updatedOrder;
        }

        public async Task<bool> CancelOrderAsync(int orderId)
        {
            var order = await GetOrderAsync(orderId);
            if (order == null)
                return false;

            if (order.Status != OrderStatus.Pending)
            {
                _logger.LogWarning($"Cannot cancel order {orderId} with status {order.Status}");
                return false;
            }

            order.Status = OrderStatus.Cancelled;
            order.CancelledDate = DateTime.UtcNow;
            order.UpdatedOn = DateTime.UtcNow;

            await UpdateOrderAsync(order);

            // Update ticket inventory
            await _ticketInventoryHub.Clients.All
                .SendAsync("InventoryUpdated", order.EventId);

            return true;
        }

        public async Task<bool> ProcessPaymentAsync(int orderId, string paymentIntentId)
        {
            var order = await GetOrderAsync(orderId);
            if (order == null)
                return false;

            order.Status = OrderStatus.Processing;
            order.PaymentIntentId = paymentIntentId;
            order.UpdatedOn = DateTime.UtcNow;

            await UpdateOrderAsync(order);
            return true;
        }

        public async Task<bool> RefundOrderAsync(int orderId, decimal? refundAmount = null)
        {
            var order = await GetOrderAsync(orderId);
            if (order == null)
                return false;

            if (order.Status != OrderStatus.Completed)
            {
                _logger.LogWarning($"Cannot refund order {orderId} with status {order.Status}");
                return false;
            }

            order.Status = OrderStatus.Refunded;
            order.RefundedDate = DateTime.UtcNow;
            order.UpdatedOn = DateTime.UtcNow;

            await UpdateOrderAsync(order);

            // Update tickets status
            var orderItems = await _orderRepository.GetOrderItemsAsync(orderId);
            foreach (var item in orderItems)
            {
                foreach (var ticket in item.Tickets)
                {
                    ticket.Status = TicketStatus.Refunded;
                    ticket.UpdatedOn = DateTime.UtcNow;
                }
            }

            return true;
        }

        public async Task<OrderSummary> CalculateOrderSummaryAsync(List<OrderItemRequest> items)
        {
            var summary = new OrderSummary
            {
                ItemPrices = new Dictionary<int, decimal>()
            };

            decimal subTotal = 0;

            foreach (var item in items)
            {
                // TODO: Get ticket price from database
                decimal price = 50.00m; // Placeholder price
                summary.ItemPrices[item.TicketTypeId] = price;
                subTotal += price * item.Quantity;
            }

            summary.SubTotal = subTotal;
            summary.TaxAmount = subTotal * 0.08m; // 8% tax
            summary.ServiceFee = subTotal * 0.10m; // 10% service fee
            summary.DiscountAmount = 0; // TODO: Apply discount codes
            summary.TotalAmount = summary.SubTotal + summary.TaxAmount + summary.ServiceFee - summary.DiscountAmount;

            return summary;
        }

        public async Task<string> GenerateOrderNumberAsync()
        {
            // Let the repository handle the unique generation
            return string.Empty;
        }

        public async Task<bool> ValidateOrderAsync(int orderId)
        {
            var order = await GetOrderAsync(orderId);
            if (order == null)
                return false;

            // TODO: Add validation logic
            return true;
        }
    }

    public class OrderSummary
    {
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public Dictionary<int, decimal> ItemPrices { get; set; } = new();
    }

    public class BillingAddress
    {
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }
}