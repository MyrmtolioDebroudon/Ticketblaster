using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Infrastructure
{
    [Authorize]
    public class OrderStatusHub : Hub
    {
        public async Task JoinOrderGroup(string orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Order_{orderId}");
        }

        public async Task LeaveOrderGroup(string orderId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Order_{orderId}");
        }

        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }

        public async Task LeaveUserGroup(string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }

    public static class OrderStatusHubExtensions
    {
        public static async Task NotifyOrderStatusUpdate(this IHubContext<OrderStatusHub> hubContext, int orderId, int userId, OrderStatus status, string message = "")
        {
            var notification = new
            {
                OrderId = orderId,
                Status = status.ToString(),
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            await hubContext.Clients.Group($"Order_{orderId}").SendAsync("OrderStatusUpdated", notification);
            await hubContext.Clients.Group($"User_{userId}").SendAsync("OrderStatusUpdated", notification);
        }

        public static async Task NotifyPaymentStatusUpdate(this IHubContext<OrderStatusHub> hubContext, int orderId, int userId, PaymentStatus status, string message = "")
        {
            var notification = new
            {
                OrderId = orderId,
                PaymentStatus = status.ToString(),
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            await hubContext.Clients.Group($"Order_{orderId}").SendAsync("PaymentStatusUpdated", notification);
            await hubContext.Clients.Group($"User_{userId}").SendAsync("PaymentStatusUpdated", notification);
        }

        public static async Task NotifyTicketDelivery(this IHubContext<OrderStatusHub> hubContext, int orderId, int userId, List<string> ticketNumbers)
        {
            var notification = new
            {
                OrderId = orderId,
                TicketNumbers = ticketNumbers,
                Message = "Your tickets have been generated and are ready for download.",
                Timestamp = DateTime.UtcNow
            };

            await hubContext.Clients.Group($"Order_{orderId}").SendAsync("TicketsDelivered", notification);
            await hubContext.Clients.Group($"User_{userId}").SendAsync("TicketsDelivered", notification);
        }

        public static async Task NotifyRefundProcessed(this IHubContext<OrderStatusHub> hubContext, int orderId, int userId, decimal refundAmount)
        {
            var notification = new
            {
                OrderId = orderId,
                RefundAmount = refundAmount,
                Message = $"Refund of ${refundAmount:F2} has been processed.",
                Timestamp = DateTime.UtcNow
            };

            await hubContext.Clients.Group($"Order_{orderId}").SendAsync("RefundProcessed", notification);
            await hubContext.Clients.Group($"User_{userId}").SendAsync("RefundProcessed", notification);
        }

        public static async Task NotifyOrderCancellation(this IHubContext<OrderStatusHub> hubContext, int orderId, int userId, string reason)
        {
            var notification = new
            {
                OrderId = orderId,
                Reason = reason,
                Message = "Your order has been cancelled.",
                Timestamp = DateTime.UtcNow
            };

            await hubContext.Clients.Group($"Order_{orderId}").SendAsync("OrderCancelled", notification);
            await hubContext.Clients.Group($"User_{userId}").SendAsync("OrderCancelled", notification);
        }
    }
}