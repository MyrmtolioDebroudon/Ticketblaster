using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace TicketBlaster.Server.Infrastructure
{
    [Authorize]
    public class TicketInventoryHub : Hub
    {
        public async Task JoinEventGroup(string eventId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Event_{eventId}");
        }

        public async Task LeaveEventGroup(string eventId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Event_{eventId}");
        }

        public async Task JoinTicketTypeGroup(string ticketTypeId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"TicketType_{ticketTypeId}");
        }

        public async Task LeaveTicketTypeGroup(string ticketTypeId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"TicketType_{ticketTypeId}");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Clean up any reservations or temporary holds
            await base.OnDisconnectedAsync(exception);
        }
    }

    public static class TicketInventoryHubExtensions
    {
        public static async Task NotifyInventoryUpdate(this IHubContext<TicketInventoryHub> hubContext, int eventId, int ticketTypeId, int availableQuantity)
        {
            await hubContext.Clients.Group($"Event_{eventId}").SendAsync("InventoryUpdated", new
            {
                TicketTypeId = ticketTypeId,
                AvailableQuantity = availableQuantity,
                Timestamp = DateTime.UtcNow
            });

            await hubContext.Clients.Group($"TicketType_{ticketTypeId}").SendAsync("InventoryUpdated", new
            {
                TicketTypeId = ticketTypeId,
                AvailableQuantity = availableQuantity,
                Timestamp = DateTime.UtcNow
            });
        }

        public static async Task NotifyTicketReservation(this IHubContext<TicketInventoryHub> hubContext, int eventId, int ticketTypeId, int quantity, string userId)
        {
            await hubContext.Clients.Group($"Event_{eventId}").SendAsync("TicketReserved", new
            {
                TicketTypeId = ticketTypeId,
                Quantity = quantity,
                UserId = userId,
                Timestamp = DateTime.UtcNow
            });
        }

        public static async Task NotifyTicketPurchase(this IHubContext<TicketInventoryHub> hubContext, int eventId, int ticketTypeId, int quantity)
        {
            await hubContext.Clients.Group($"Event_{eventId}").SendAsync("TicketPurchased", new
            {
                TicketTypeId = ticketTypeId,
                Quantity = quantity,
                Timestamp = DateTime.UtcNow
            });
        }

        public static async Task NotifyLowInventory(this IHubContext<TicketInventoryHub> hubContext, int eventId, int ticketTypeId, int remainingQuantity)
        {
            await hubContext.Clients.Group($"Event_{eventId}").SendAsync("LowInventoryAlert", new
            {
                TicketTypeId = ticketTypeId,
                RemainingQuantity = remainingQuantity,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}