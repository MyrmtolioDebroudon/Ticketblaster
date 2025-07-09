using Microsoft.EntityFrameworkCore;
using TicketBlaster.Database;
using TicketBlaster.Server.Repository.Interfaces;
using TicketBlaster.Shared.Models;
using System.Security.Cryptography;

namespace TicketBlaster.Server.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly TicketBlasterDbContext _dbContext;

        public OrderRepository(TicketBlasterDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Order?> GetOrderAsync(int orderId)
        {
            return await _dbContext.Orders
                .Include(o => o.Event)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.TicketType)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Tickets)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
        {
            return await _dbContext.Orders
                .Include(o => o.Event)
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.TicketType)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Tickets)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        }

        public async Task<IEnumerable<Order>> GetUserOrdersAsync(int userId)
        {
            return await _dbContext.Orders
                .Include(o => o.Event)
                .Include(o => o.OrderItems)
                .Include(o => o.Payments)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetEventOrdersAsync(int eventId)
        {
            return await _dbContext.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .Include(o => o.Payments)
                .Where(o => o.EventId == eventId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            // Generate unique order number if not provided
            if (string.IsNullOrEmpty(order.OrderNumber))
            {
                order.OrderNumber = await GenerateUniqueOrderNumberAsync();
            }

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();
            return order;
        }

        public async Task<Order> UpdateOrderAsync(Order order)
        {
            order.UpdatedOn = DateTime.UtcNow;
            _dbContext.Orders.Update(order);
            await _dbContext.SaveChangesAsync();
            return order;
        }

        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            var order = await GetOrderAsync(orderId);
            if (order == null)
                return false;

            _dbContext.Orders.Remove(order);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<OrderItem>> GetOrderItemsAsync(int orderId)
        {
            return await _dbContext.OrderItems
                .Include(oi => oi.TicketType)
                .Include(oi => oi.Tickets)
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();
        }

        public async Task<OrderItem> CreateOrderItemAsync(OrderItem orderItem)
        {
            _dbContext.OrderItems.Add(orderItem);
            await _dbContext.SaveChangesAsync();
            return orderItem;
        }

        public async Task<IEnumerable<Ticket>> GenerateTicketsAsync(int orderItemId, int quantity)
        {
            var tickets = new List<Ticket>();
            var orderItem = await _dbContext.OrderItems
                .Include(oi => oi.Order)
                    .ThenInclude(o => o.User)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId);

            if (orderItem == null)
                return tickets;

            for (int i = 0; i < quantity; i++)
            {
                var ticket = new Ticket
                {
                    OrderItemId = orderItemId,
                    TicketNumber = await GenerateUniqueTicketNumberAsync(),
                    QRCode = GenerateQRCode(),
                    AttendeeFirstName = orderItem.Order.CustomerFirstName,
                    AttendeeLastName = orderItem.Order.CustomerLastName,
                    AttendeeEmail = orderItem.Order.CustomerEmail,
                    Status = TicketStatus.Active,
                    CreatedOn = DateTime.UtcNow
                };

                tickets.Add(ticket);
            }

            _dbContext.Tickets.AddRange(tickets);
            await _dbContext.SaveChangesAsync();

            return tickets;
        }

        private async Task<string> GenerateUniqueOrderNumberAsync()
        {
            string orderNumber;
            bool exists;

            do
            {
                // Generate order number format: TB-YYYYMMDD-XXXXX
                var date = DateTime.UtcNow.ToString("yyyyMMdd");
                var random = new Random().Next(10000, 99999);
                orderNumber = $"TB-{date}-{random}";

                exists = await _dbContext.Orders.AnyAsync(o => o.OrderNumber == orderNumber);
            } while (exists);

            return orderNumber;
        }

        private async Task<string> GenerateUniqueTicketNumberAsync()
        {
            string ticketNumber;
            bool exists;

            do
            {
                // Generate ticket number format: TKT-XXXXXXXXXX
                var bytes = new byte[8];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(bytes);
                }
                ticketNumber = $"TKT-{Convert.ToBase64String(bytes).Replace("/", "").Replace("+", "").Replace("=", "").Substring(0, 10)}";

                exists = await _dbContext.Tickets.AnyAsync(t => t.TicketNumber == ticketNumber);
            } while (exists);

            return ticketNumber;
        }

        private string GenerateQRCode()
        {
            // Generate unique QR code data
            var guid = Guid.NewGuid().ToString();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return $"TB-{guid}-{timestamp}";
        }
    }
}