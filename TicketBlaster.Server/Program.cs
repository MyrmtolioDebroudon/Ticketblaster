using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using TicketBlaster.Database;
using TicketBlaster.Server.Services;
using TicketBlaster.Server.Infrastructure;
using Oqtane.Infrastructure;
using Oqtane.Repository;
using Oqtane.Security;
using Oqtane.Services;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add Entity Framework
builder.Services.AddDbContext<TicketBlasterDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Oqtane framework services
builder.Services.AddOqtane(builder.Configuration);

// Add Authentication services
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = builder.Configuration["Keycloak:Authority"];
    options.Audience = builder.Configuration["Keycloak:Audience"];
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero
    };
})
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Keycloak:Authority"];
    options.ClientId = builder.Configuration["Keycloak:ClientId"];
    options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
});

// Add Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("OrganizerOnly", policy => policy.RequireRole("Organizer"));
    options.AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer"));
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Add FluentValidation - commented out for now
// builder.Services.AddFluentValidationAutoValidation();

// Add Stripe services - commented out for now
// builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

// Add our custom services
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IEventSearchService, EventSearchService>();
builder.Services.AddScoped<IEventDiscoveryService, EventDiscoveryService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IKeycloakService, KeycloakService>();

// Add HTTP client for external API calls
builder.Services.AddHttpClient();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder =>
        {
            builder.WithOrigins("https://localhost:44354")
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials();
        });
});

// Add API versioning - commented out for now
// builder.Services.AddApiVersioning(options =>
// {
//     options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
//     options.AssumeDefaultVersionWhenUnspecified = true;
// });

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TicketBlaster API",
        Version = "v1",
        Description = "Event ticketing platform API"
    });
});

// Add SignalR for real-time updates
builder.Services.AddSignalR();

// Add caching
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TicketBlasterDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowSpecificOrigin");

app.UseAuthentication();
app.UseAuthorization();

app.UseOqtane();

app.MapRazorPages();
app.MapBlazorHub();
app.MapControllers();
app.MapHealthChecks("/health");

// Map SignalR hubs
app.MapHub<TicketInventoryHub>("/ticketInventoryHub");
app.MapHub<OrderStatusHub>("/orderStatusHub");

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TicketBlasterDbContext>();
    try
    {
        context.Database.EnsureCreated();
        Log.Information("Database created successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error creating database");
    }
}

Log.Information("TicketBlaster Server starting up");

app.Run();

// Configuration classes
// public class StripeSettings
// {
//     public string SecretKey { get; set; } = string.Empty;
//     public string PublishableKey { get; set; } = string.Empty;
//     public string WebhookSecret { get; set; } = string.Empty;
// }