using Microsoft.EntityFrameworkCore;
using TicketBlaster.Database;
using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public class EventService : IEventService
    {
        private readonly TicketBlasterDbContext _context;
        private readonly ILogger<EventService> _logger;

        public EventService(TicketBlasterDbContext context, ILogger<EventService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Event?> GetEventAsync(int eventId)
        {
            return await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .FirstOrDefaultAsync(e => e.EventId == eventId);
        }

        public async Task<Event?> GetEventByCustomUrlAsync(string customUrl)
        {
            return await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .FirstOrDefaultAsync(e => e.CustomUrl == customUrl);
        }

        public async Task<IEnumerable<Event>> GetEventsAsync(int organizerId)
        {
            return await _context.Events
                .Include(e => e.Category)
                .Include(e => e.TicketTypes)
                .Where(e => e.OrganizerId == organizerId)
                .OrderByDescending(e => e.StartDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetPublicEventsAsync(int pageNumber = 1, int pageSize = 20)
        {
            var skip = (pageNumber - 1) * pageSize;
            
            return await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .Where(e => e.IsActive && e.Status == EventStatus.Published)
                .OrderBy(e => e.StartDate)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Event>> SearchEventsAsync(string searchTerm, int? categoryId = null, 
            DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .Where(e => e.IsActive && e.Status == EventStatus.Published);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                query = query.Where(e => 
                    e.Title.ToLower().Contains(searchLower) ||
                    e.Description.ToLower().Contains(searchLower) ||
                    e.Location.ToLower().Contains(searchLower));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(e => e.CategoryId == categoryId.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(e => e.EndDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(e => e.StartDate <= endDate.Value);
            }

            return await query
                .OrderBy(e => e.StartDate)
                .Take(50)
                .ToListAsync();
        }

        public async Task<Event> CreateEventAsync(Event eventEntity)
        {
            try
            {
                eventEntity.CreatedOn = DateTime.UtcNow;
                eventEntity.IsActive = true;
                
                _context.Events.Add(eventEntity);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Event created: {EventId} - {Title}", eventEntity.EventId, eventEntity.Title);
                return eventEntity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event: {Title}", eventEntity.Title);
                throw;
            }
        }

        public async Task<Event> UpdateEventAsync(Event eventEntity)
        {
            try
            {
                var existingEvent = await _context.Events.FindAsync(eventEntity.EventId);
                if (existingEvent == null)
                {
                    throw new InvalidOperationException($"Event with ID {eventEntity.EventId} not found");
                }

                // Update properties
                existingEvent.Title = eventEntity.Title;
                existingEvent.Description = eventEntity.Description;
                existingEvent.StartDate = eventEntity.StartDate;
                existingEvent.EndDate = eventEntity.EndDate;
                existingEvent.Location = eventEntity.Location;
                existingEvent.VirtualUrl = eventEntity.VirtualUrl;
                existingEvent.ImageUrl = eventEntity.ImageUrl;
                existingEvent.Status = eventEntity.Status;
                existingEvent.CategoryId = eventEntity.CategoryId;
                existingEvent.MaxCapacity = eventEntity.MaxCapacity;
                existingEvent.CustomUrl = eventEntity.CustomUrl;
                existingEvent.Tags = eventEntity.Tags;
                existingEvent.IsRecurring = eventEntity.IsRecurring;
                existingEvent.IsMultiDay = eventEntity.IsMultiDay;
                existingEvent.IsVirtual = eventEntity.IsVirtual;
                existingEvent.IsFeatured = eventEntity.IsFeatured;
                existingEvent.UpdatedOn = DateTime.UtcNow;
                existingEvent.UpdatedBy = eventEntity.UpdatedBy;

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Event updated: {EventId} - {Title}", existingEvent.EventId, existingEvent.Title);
                return existingEvent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event: {EventId}", eventEntity.EventId);
                throw;
            }
        }

        public async Task<bool> DeleteEventAsync(int eventId)
        {
            try
            {
                var eventEntity = await _context.Events.FindAsync(eventId);
                if (eventEntity == null)
                {
                    return false;
                }

                // Soft delete
                eventEntity.IsActive = false;
                eventEntity.UpdatedOn = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Event deleted: {EventId} - {Title}", eventEntity.EventId, eventEntity.Title);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event: {EventId}", eventId);
                throw;
            }
        }

        public async Task<bool> PublishEventAsync(int eventId)
        {
            try
            {
                var eventEntity = await _context.Events.FindAsync(eventId);
                if (eventEntity == null)
                {
                    return false;
                }

                eventEntity.Status = EventStatus.Published;
                eventEntity.UpdatedOn = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Event published: {EventId} - {Title}", eventEntity.EventId, eventEntity.Title);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing event: {EventId}", eventId);
                throw;
            }
        }

        public async Task<bool> CancelEventAsync(int eventId)
        {
            try
            {
                var eventEntity = await _context.Events.FindAsync(eventId);
                if (eventEntity == null)
                {
                    return false;
                }

                eventEntity.Status = EventStatus.Cancelled;
                eventEntity.UpdatedOn = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Event cancelled: {EventId} - {Title}", eventEntity.EventId, eventEntity.Title);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling event: {EventId}", eventId);
                throw;
            }
        }

        public async Task<IEnumerable<Event>> GetFeaturedEventsAsync(int count = 10)
        {
            return await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .Where(e => e.IsActive && e.Status == EventStatus.Published && e.IsFeatured)
                .OrderBy(e => e.StartDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetEventsByCategoryAsync(int categoryId, int pageNumber = 1, int pageSize = 20)
        {
            var skip = (pageNumber - 1) * pageSize;
            
            return await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .Where(e => e.IsActive && e.Status == EventStatus.Published && e.CategoryId == categoryId)
                .OrderBy(e => e.StartDate)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> IsEventOrganizerAsync(int eventId, int userId)
        {
            var eventEntity = await _context.Events
                .Where(e => e.EventId == eventId)
                .Select(e => new { e.OrganizerId })
                .FirstOrDefaultAsync();
                
            return eventEntity?.OrganizerId == userId;
        }

        public async Task<EventStatistics> GetEventStatisticsAsync(int eventId)
        {
            var eventData = await _context.Events
                .Include(e => e.Orders)
                    .ThenInclude(o => o.OrderItems)
                .Include(e => e.TicketTypes)
                .Where(e => e.EventId == eventId)
                .FirstOrDefaultAsync();

            if (eventData == null)
            {
                return new EventStatistics();
            }

            var completedOrders = eventData.Orders.Where(o => o.Status == OrderStatus.Completed).ToList();
            
            var statistics = new EventStatistics
            {
                TotalTicketsSold = completedOrders.Sum(o => o.OrderItems.Sum(oi => oi.Quantity)),
                TotalRevenue = completedOrders.Sum(o => o.TotalAmount),
                TotalOrders = completedOrders.Count,
                TotalViews = 0 // This would come from analytics
            };

            // Ticket sales by type
            statistics.TicketSalesByType = eventData.TicketTypes
                .ToDictionary(
                    tt => tt.Name,
                    tt => completedOrders
                        .SelectMany(o => o.OrderItems)
                        .Where(oi => oi.TicketTypeId == tt.TicketTypeId)
                        .Sum(oi => oi.Quantity)
                );

            // Daily sales for the last 30 days
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            statistics.DailySales = completedOrders
                .Where(o => o.OrderDate >= thirtyDaysAgo)
                .GroupBy(o => o.OrderDate.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(o => o.OrderItems.Sum(oi => oi.Quantity))
                );

            return statistics;
        }
    }
}