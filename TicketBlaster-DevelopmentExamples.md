# TicketBlaster Development Examples
## Real Conversations & Code Generation with Claude

This document provides actual examples of how Claude Code and background agents were used to build the TicketBlaster platform.

---

## Example 1: Initial Project Setup

### User Prompt:
```
I need to create a ticketing platform using Oqtane framework with .NET 8. 
Can you help me set up the initial project structure with proper separation 
of concerns? Include Server, Client, Shared, and Database projects.
```

### Claude's Response & Actions:

1. **Created Solution Structure:**
```bash
TicketBlaster.Solution/
├── TicketBlaster.sln
├── TicketBlaster.Server/
├── TicketBlaster.Client/
├── TicketBlaster.Shared/
├── TicketBlaster.Database/
└── TicketBlaster.Modules/
```

2. **Generated Solution File:**
```xml
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
...
```

3. **Created Initial README:**
```markdown
# TicketBlaster - Open-Source Ticketing Platform
A comprehensive event ticketing platform built with Oqtane 6.1.3+...
```

---

## Example 2: Database Model Generation

### User Prompt:
```
@Claude: Create the Event model with all necessary properties for a ticketing 
system. Include support for virtual events, multi-day events, categories, 
and capacity tracking.
```

### Generated Code:
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Oqtane.Shared;

namespace TicketBlaster.Shared.Models
{
    public class Event : ModelBase
    {
        [Key]
        public int EventId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(5000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [StringLength(500)]
        public string Location { get; set; } = string.Empty;

        [StringLength(500)]
        public string VirtualUrl { get; set; } = string.Empty;

        // ... additional properties

        // Navigation properties
        public virtual Category Category { get; set; } = null!;
        public virtual User Organizer { get; set; } = null!;
        public virtual ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();
    }
}
```

---

## Example 3: Service Interface Design

### User Prompt:
```
Design a comprehensive IEventService interface that handles all event-related 
operations including CRUD, search, filtering by category/date, and checking 
event capacity.
```

### Claude Generated:
```csharp
using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public interface IEventService
    {
        // Basic CRUD operations
        Task<Event?> GetEventAsync(int eventId);
        Task<Event> CreateEventAsync(Event eventItem);
        Task<Event> UpdateEventAsync(Event eventItem);
        Task<bool> DeleteEventAsync(int eventId);

        // Search and filtering
        Task<IEnumerable<Event>> GetEventsAsync(
            string? searchTerm = null,
            int? categoryId = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            EventStatus? status = null,
            int pageNumber = 1,
            int pageSize = 20);

        // Specialized queries
        Task<IEnumerable<Event>> GetUpcomingEventsAsync(int count = 10);
        Task<IEnumerable<Event>> GetEventsByOrganizerAsync(int organizerId);
        Task<IEnumerable<Event>> GetFeaturedEventsAsync();

        // Capacity management
        Task<bool> CheckEventCapacityAsync(int eventId, int requestedTickets);
        Task<bool> UpdateEventCapacityAsync(int eventId, int soldTickets);

        // Event operations
        Task<bool> PublishEventAsync(int eventId);
        Task<bool> CancelEventAsync(int eventId, string reason);
        Task<bool> PostponeEventAsync(int eventId, DateTime newStartDate, DateTime newEndDate);

        // Statistics
        Task<EventStatistics> GetEventStatisticsAsync(int eventId);
        Task<decimal> GetEventRevenueAsync(int eventId);
    }
}
```

---

## Example 4: SignalR Hub Implementation

### Background Agent Task:
```
Implement a SignalR hub for real-time ticket inventory updates. When tickets 
are purchased or released, all connected clients should receive updates about 
the new availability.
```

### Generated Implementation:
```csharp
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Infrastructure
{
    public class TicketInventoryHub : Hub
    {
        private readonly ITicketService _ticketService;
        private readonly ILogger<TicketInventoryHub> _logger;

        public TicketInventoryHub(
            ITicketService ticketService,
            ILogger<TicketInventoryHub> logger)
        {
            _ticketService = ticketService;
            _logger = logger;
        }

        public async Task JoinEventGroup(int eventId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"event-{eventId}");
            _logger.LogInformation($"Client {Context.ConnectionId} joined event {eventId}");

            // Send current inventory status
            var ticketTypes = await _ticketService.GetEventTicketTypesAsync(eventId);
            await Clients.Caller.SendAsync("ReceiveInventoryUpdate", ticketTypes);
        }

        public async Task LeaveEventGroup(int eventId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"event-{eventId}");
            _logger.LogInformation($"Client {Context.ConnectionId} left event {eventId}");
        }

        // Called by the service layer when inventory changes
        public async Task NotifyInventoryChange(int eventId, TicketTypeInventoryUpdate update)
        {
            await Clients.Group($"event-{eventId}")
                .SendAsync("ReceiveInventoryUpdate", update);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"Client {Context.ConnectionId} disconnected");
            await base.OnDisconnectedAsync(exception);
        }
    }

    public class TicketTypeInventoryUpdate
    {
        public int TicketTypeId { get; set; }
        public int AvailableQuantity { get; set; }
        public int SoldQuantity { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
```

---

## Example 5: Keycloak Authentication Setup

### User Prompt:
```
Background Agent: Set up Keycloak authentication for the TicketBlaster 
platform. Create the authentication module, configure JWT validation, 
and implement role-based authorization.
```

### Generated Docker Compose:
```yaml
version: '3.8'

services:
  keycloak:
    image: quay.io/keycloak/keycloak:latest
    container_name: ticketblaster-keycloak
    environment:
      - KEYCLOAK_ADMIN=admin
      - KEYCLOAK_ADMIN_PASSWORD=admin
      - KC_DB=postgres
      - KC_DB_URL=jdbc:postgresql://keycloak-db:5432/keycloak
      - KC_DB_USERNAME=keycloak
      - KC_DB_PASSWORD=keycloak
    ports:
      - "8080:8080"
    depends_on:
      - keycloak-db
    command: start-dev

  keycloak-db:
    image: postgres:15
    container_name: ticketblaster-keycloak-db
    environment:
      - POSTGRES_DB=keycloak
      - POSTGRES_USER=keycloak
      - POSTGRES_PASSWORD=keycloak
    volumes:
      - keycloak-db-data:/var/lib/postgresql/data

volumes:
  keycloak-db-data:
```

### Generated Authentication Configuration:
```csharp
// In Program.cs
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = builder.Configuration["Keycloak:Authority"];
    options.Audience = builder.Configuration["Keycloak:Audience"];
    options.RequireHttpsMetadata = false; // Only for development

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Keycloak:Authority"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Keycloak:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();
            logger.LogError("Authentication failed: {Error}", context.Exception);
            return Task.CompletedTask;
        }
    };
});

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => 
        policy.RequireRole("admin"));
    
    options.AddPolicy("Organizer", policy => 
        policy.RequireRole("organizer", "admin"));
    
    options.AddPolicy("Customer", policy => 
        policy.RequireAuthenticatedUser());
});
```

---

## Example 6: Stripe Payment Integration

### User Prompt:
```
@Claude: Implement Stripe payment processing for ticket purchases. Include 
payment intent creation, webhook handling for confirmations, and support 
for refunds.
```

### Generated Payment Service:
```csharp
using Stripe;
using TicketBlaster.Shared.Models;

namespace TicketBlaster.Server.Services
{
    public class StripePaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly IOrderService _orderService;
        private readonly ILogger<StripePaymentService> _logger;

        public StripePaymentService(
            IConfiguration configuration,
            IOrderService orderService,
            ILogger<StripePaymentService> logger)
        {
            _configuration = configuration;
            _orderService = orderService;
            _logger = logger;
            
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public async Task<PaymentIntent> CreatePaymentIntentAsync(Order order)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(order.TotalAmount * 100), // Convert to cents
                Currency = "usd",
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
                Metadata = new Dictionary<string, string>
                {
                    ["orderId"] = order.OrderId.ToString(),
                    ["customerEmail"] = order.CustomerEmail
                }
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            // Update order with payment intent ID
            order.PaymentIntentId = paymentIntent.Id;
            await _orderService.UpdateOrderAsync(order);

            _logger.LogInformation($"Created payment intent {paymentIntent.Id} for order {order.OrderId}");

            return paymentIntent;
        }

        public async Task<bool> HandleWebhookAsync(string json, string signature)
        {
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    signature,
                    _configuration["Stripe:WebhookSecret"]
                );

                switch (stripeEvent.Type)
                {
                    case "payment_intent.succeeded":
                        await HandlePaymentSucceededAsync(stripeEvent);
                        break;
                    case "payment_intent.payment_failed":
                        await HandlePaymentFailedAsync(stripeEvent);
                        break;
                    default:
                        _logger.LogWarning($"Unhandled Stripe event type: {stripeEvent.Type}");
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Stripe webhook");
                return false;
            }
        }

        private async Task HandlePaymentSucceededAsync(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null) return;

            var orderId = int.Parse(paymentIntent.Metadata["orderId"]);
            await _orderService.ConfirmOrderPaymentAsync(orderId, paymentIntent.Id);
            
            _logger.LogInformation($"Payment succeeded for order {orderId}");
        }

        // Additional methods...
    }
}
```

---

## Example 7: Blazor Component Development

### User Prompt:
```
Create a Blazor component for displaying events in a grid with filtering 
by category, date range, and search. Include real-time availability updates.
```

### Generated Component:
```razor
@page "/events"
@using Microsoft.AspNetCore.SignalR.Client
@implements IAsyncDisposable
@inject IEventService EventService
@inject NavigationManager Navigation

<PageTitle>Browse Events</PageTitle>

<div class="container-fluid">
    <div class="row">
        <!-- Filters sidebar -->
        <div class="col-md-3">
            <div class="card">
                <div class="card-header">
                    <h5>Filters</h5>
                </div>
                <div class="card-body">
                    <!-- Search -->
                    <div class="mb-3">
                        <input type="text" class="form-control" placeholder="Search events..."
                               @bind="searchTerm" @bind:event="oninput" 
                               @onkeyup="@(async (e) => { if (e.Key == "Enter") await SearchEvents(); })" />
                    </div>

                    <!-- Category filter -->
                    <div class="mb-3">
                        <label class="form-label">Category</label>
                        <select class="form-select" @bind="selectedCategoryId" @bind:event="onchange" @onchange="SearchEvents">
                            <option value="">All Categories</option>
                            @foreach (var category in categories)
                            {
                                <option value="@category.CategoryId">@category.Name</option>
                            }
                        </select>
                    </div>

                    <!-- Date range -->
                    <div class="mb-3">
                        <label class="form-label">Start Date</label>
                        <input type="date" class="form-control" @bind="startDate" @bind:event="onchange" @onchange="SearchEvents" />
                    </div>

                    <div class="mb-3">
                        <label class="form-label">End Date</label>
                        <input type="date" class="form-control" @bind="endDate" @bind:event="onchange" @onchange="SearchEvents" />
                    </div>

                    <button class="btn btn-primary w-100" @onclick="SearchEvents">
                        <i class="fas fa-search"></i> Search
                    </button>
                </div>
            </div>
        </div>

        <!-- Events grid -->
        <div class="col-md-9">
            @if (isLoading)
            {
                <div class="text-center py-5">
                    <div class="spinner-border" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                </div>
            }
            else if (!events.Any())
            {
                <div class="alert alert-info">
                    No events found matching your criteria.
                </div>
            }
            else
            {
                <div class="row g-4">
                    @foreach (var eventItem in events)
                    {
                        <div class="col-md-6 col-lg-4">
                            <EventCard Event="eventItem" OnClick="() => NavigateToEvent(eventItem.EventId)" />
                        </div>
                    }
                </div>

                <!-- Pagination -->
                <nav aria-label="Event pagination" class="mt-4">
                    <ul class="pagination justify-content-center">
                        <li class="page-item @(currentPage == 1 ? "disabled" : "")">
                            <button class="page-link" @onclick="() => LoadPage(currentPage - 1)">Previous</button>
                        </li>
                        @for (int i = 1; i <= totalPages; i++)
                        {
                            var pageNum = i;
                            <li class="page-item @(currentPage == pageNum ? "active" : "")">
                                <button class="page-link" @onclick="() => LoadPage(pageNum)">@pageNum</button>
                            </li>
                        }
                        <li class="page-item @(currentPage == totalPages ? "disabled" : "")">
                            <button class="page-link" @onclick="() => LoadPage(currentPage + 1)">Next</button>
                        </li>
                    </ul>
                </nav>
            }
        </div>
    </div>
</div>

@code {
    private HubConnection? hubConnection;
    private List<Event> events = new();
    private List<Category> categories = new();
    private bool isLoading = true;
    
    // Filter parameters
    private string searchTerm = "";
    private int? selectedCategoryId;
    private DateTime? startDate;
    private DateTime? endDate;
    
    // Pagination
    private int currentPage = 1;
    private int pageSize = 12;
    private int totalPages = 1;

    protected override async Task OnInitializedAsync()
    {
        // Set up SignalR connection for real-time updates
        hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/ticketInventoryHub"))
            .WithAutomaticReconnect()
            .Build();

        hubConnection.On<TicketTypeInventoryUpdate>("ReceiveInventoryUpdate", (update) =>
        {
            InvokeAsync(() =>
            {
                UpdateEventAvailability(update);
                StateHasChanged();
            });
        });

        await hubConnection.StartAsync();

        // Load initial data
        categories = await EventService.GetCategoriesAsync();
        await LoadEvents();
    }

    private async Task LoadEvents()
    {
        isLoading = true;
        
        var result = await EventService.GetEventsAsync(
            searchTerm, 
            selectedCategoryId, 
            startDate, 
            endDate,
            EventStatus.Published,
            currentPage, 
            pageSize);
            
        events = result.Items.ToList();
        totalPages = result.TotalPages;
        
        // Join SignalR groups for each visible event
        foreach (var e in events)
        {
            await hubConnection.SendAsync("JoinEventGroup", e.EventId);
        }
        
        isLoading = false;
    }

    private void UpdateEventAvailability(TicketTypeInventoryUpdate update)
    {
        // Update the availability display for the affected event
        var affectedEvent = events.FirstOrDefault(e => 
            e.TicketTypes.Any(tt => tt.TicketTypeId == update.TicketTypeId));
            
        if (affectedEvent != null)
        {
            var ticketType = affectedEvent.TicketTypes
                .First(tt => tt.TicketTypeId == update.TicketTypeId);
            
            ticketType.AvailableQuantity = update.AvailableQuantity;
            ticketType.SoldQuantity = update.SoldQuantity;
        }
    }

    private async Task SearchEvents()
    {
        currentPage = 1;
        await LoadEvents();
    }

    private async Task LoadPage(int page)
    {
        if (page < 1 || page > totalPages) return;
        currentPage = page;
        await LoadEvents();
    }

    private void NavigateToEvent(int eventId)
    {
        Navigation.NavigateTo($"/events/{eventId}");
    }

    public async ValueTask DisposeAsync()
    {
        if (hubConnection is not null)
        {
            // Leave all event groups
            foreach (var e in events)
            {
                await hubConnection.SendAsync("LeaveEventGroup", e.EventId);
            }
            
            await hubConnection.DisposeAsync();
        }
    }
}
```

---

## Example 8: Error Handling and Debugging

### Scenario: Claude-generated code had a bug

#### Initial Generated Code (with bug):
```csharp
public async Task<Order> CreateOrderAsync(int userId, List<OrderItem> items)
{
    var order = new Order
    {
        UserId = userId,
        OrderItems = items,
        TotalAmount = items.Sum(i => i.Price), // Bug: not multiplying by quantity
        Status = OrderStatus.Pending
    };
    
    await _context.Orders.AddAsync(order);
    await _context.SaveChangesAsync();
    return order;
}
```

#### User Feedback:
```
The total amount calculation is wrong. It's not considering the quantity 
of tickets in each order item.
```

#### Claude's Fixed Code:
```csharp
public async Task<Order> CreateOrderAsync(int userId, List<OrderItem> items)
{
    // Validate items first
    if (items == null || !items.Any())
    {
        throw new ArgumentException("Order must contain at least one item");
    }

    // Calculate total with quantity
    var totalAmount = items.Sum(i => i.Price * i.Quantity);
    
    // Apply any discounts
    foreach (var item in items)
    {
        if (item.DiscountId.HasValue)
        {
            var discount = await _context.TicketTypeDiscounts
                .FindAsync(item.DiscountId.Value);
            
            if (discount != null && discount.IsActive)
            {
                var discountAmount = discount.Type == DiscountType.Percentage
                    ? item.Price * item.Quantity * (discount.Value / 100m)
                    : discount.Value * item.Quantity;
                    
                totalAmount -= discountAmount;
                item.DiscountAmount = discountAmount;
            }
        }
    }

    var order = new Order
    {
        UserId = userId,
        OrderNumber = GenerateOrderNumber(),
        OrderItems = items,
        SubTotal = items.Sum(i => i.Price * i.Quantity),
        DiscountTotal = items.Sum(i => i.DiscountAmount ?? 0),
        TotalAmount = totalAmount,
        Status = OrderStatus.Pending,
        CreatedOn = DateTime.UtcNow
    };
    
    try
    {
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation($"Created order {order.OrderNumber} for user {userId}");
        return order;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Failed to create order for user {userId}");
        throw;
    }
}

private string GenerateOrderNumber()
{
    return $"TB{DateTime.UtcNow:yyyyMMdd}{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
}
```

---

## Tips for Working with Claude

### 1. Iterative Refinement
- Start with basic implementation
- Test and identify issues
- Ask Claude to fix specific problems
- Request improvements and optimizations

### 2. Context Management
- Provide relevant code context
- Reference existing patterns in your codebase
- Ask for consistency with established conventions

### 3. Testing Generated Code
- Always test AI-generated code
- Ask Claude to generate unit tests
- Request error handling improvements
- Validate security considerations

### 4. Documentation
- Ask Claude to add XML documentation
- Request README updates for new features
- Generate API documentation
- Create user guides

---

## Common Patterns & Best Practices

### 1. Service Layer Pattern
Claude consistently implements:
- Interface-first design
- Dependency injection
- Async/await patterns
- Proper error handling
- Logging integration

### 2. Repository Pattern
- Generic repository base
- Specific repositories for complex queries
- Unit of Work pattern
- Query specifications

### 3. API Design
- RESTful conventions
- Consistent response formats
- Proper HTTP status codes
- API versioning
- Swagger documentation

### 4. Security Implementation
- Input validation
- Authorization checks
- SQL injection prevention
- XSS protection
- Rate limiting

---

## Conclusion

Building TicketBlaster with Claude Code and background agents demonstrates the power of AI-assisted development:

1. **Rapid Prototyping**: Complete features built in minutes instead of hours
2. **Best Practices**: Claude consistently applies industry standards
3. **Learning Tool**: Generated code serves as educational examples
4. **Productivity Boost**: Focus on business logic rather than boilerplate
5. **Quality Code**: Well-structured, documented, and tested implementations

The key to success is treating Claude as a pair programming partner - providing clear requirements, reviewing generated code, and iterating based on testing results.