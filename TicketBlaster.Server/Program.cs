using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using TicketBlaster.Database;
using TicketBlaster.Server.Services;
using TicketBlaster.Server.Infrastructure;
using TicketBlaster.Server.Security;
using TicketBlaster.Server.Validators;
using Oqtane.Infrastructure;
using Oqtane.Repository;
using Oqtane.Security;
using Oqtane.Services;
using Serilog;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure secure configuration
builder.Configuration.Sources.Clear();
builder.Configuration.AddSecureConfiguration(builder.Environment);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .CreateLogger();

builder.Host.UseSerilog();

// Validate security configuration
try
{
    SecureConfiguration.ValidateSecurityConfiguration(builder.Configuration, builder.Environment);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Security configuration validation failed");
    throw;
}

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add Entity Framework with secure connection string
var connectionString = SecureConfiguration.GetSecureValue(builder.Configuration, "ConnectionStrings:DefaultConnection");
builder.Services.AddDbContext<TicketBlasterDbContext>(options =>
{
    options.UseSqlServer(connectionString);
    
    // Enable sensitive data logging only in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
    }
});

// Add Oqtane framework services
builder.Services.AddOqtane(builder.Configuration);

// Add Authentication services with secure configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = SecureConfiguration.GetSecureValue(builder.Configuration, "Keycloak:Authority");
    options.Audience = SecureConfiguration.GetSecureValue(builder.Configuration, "Keycloak:Audience");
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,
        ValidIssuers = new[] { SecureConfiguration.GetSecureValue(builder.Configuration, "Keycloak:Authority") }
    };
    
    // Add token validation event handlers
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Warning("JWT authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Log.Information("JWT token validated for user: {User}", context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        }
    };
})
.AddOpenIdConnect(options =>
{
    options.Authority = SecureConfiguration.GetSecureValue(builder.Configuration, "Keycloak:Authority");
    options.ClientId = SecureConfiguration.GetSecureValue(builder.Configuration, "Keycloak:ClientId");
    options.ClientSecret = SecureConfiguration.GetSecureValue(builder.Configuration, "Keycloak:ClientSecret");
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.TokenValidationParameters.NameClaimType = "preferred_username";
});

// Add Authorization with enhanced policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => 
    {
        policy.RequireRole("Admin");
        policy.RequireAuthenticatedUser();
    });
    
    options.AddPolicy("OrganizerOnly", policy => 
    {
        policy.RequireRole("Admin", "Organizer");
        policy.RequireAuthenticatedUser();
    });
    
    options.AddPolicy("CustomerOnly", policy => 
    {
        policy.RequireRole("Admin", "Organizer", "Customer");
        policy.RequireAuthenticatedUser();
    });
    
    options.AddPolicy("VerifiedOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("email_verified", "true");
    });
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<EventValidator>();

// Add Stripe services with secure configuration
builder.Services.Configure<StripeSettings>(options =>
{
    options.SecretKey = SecureConfiguration.GetSecureValue(builder.Configuration, "Stripe:SecretKey");
    options.PublishableKey = SecureConfiguration.GetSecureValue(builder.Configuration, "Stripe:PublishableKey");
    options.WebhookSecret = SecureConfiguration.GetSecureValue(builder.Configuration, "Stripe:WebhookSecret");
});

// Add our custom services
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IKeycloakService, KeycloakService>();

// Add HTTP client for external API calls with security headers
builder.Services.AddHttpClient("SecureClient")
    .ConfigureHttpClient(client =>
    {
        client.DefaultRequestHeaders.Add("User-Agent", "TicketBlaster/1.0");
        client.Timeout = TimeSpan.FromSeconds(30);
    });

// Add CORS with secure configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("SecurePolicy", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "https://localhost:44354" };
            
        policy.WithOrigins(allowedOrigins)
               .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
               .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "X-CSRF-Token")
               .AllowCredentials()
               .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// Add Swagger/OpenAPI with authentication
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TicketBlaster API",
        Version = "v1",
        Description = "Event ticketing platform API",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "TicketBlaster Support",
            Email = "support@ticketblaster.com"
        }
    });
    
    // Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add SignalR for real-time updates
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 102400; // 100 KB
});

// Add caching
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TicketBlasterDbContext>(
        name: "database",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded)
    .AddUrlGroup(
        new Uri($"{builder.Configuration["Keycloak:Authority"]}/protocol/openid-connect/userinfo"),
        name: "keycloak",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);

// Add anti-forgery with secure configuration
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-Token";
    options.Cookie.Name = "__Host-X-CSRF-Token";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

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
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TicketBlaster API v1");
    });
}

// Security middleware - order matters!
app.UseSecurityHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();

// Add rate limiting
app.UseRateLimiting(options =>
{
    options.Limit = 100;
    options.Window = 60;
    options.EnableGlobalRateLimit = true;
});

app.UseRouting();

app.UseCors("SecurePolicy");

app.UseAuthentication();
app.UseAuthorization();

// Add request logging
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString());
    };
});

app.UseOqtane();

app.MapRazorPages();
app.MapBlazorHub();
app.MapControllers().RequireAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();

// Map SignalR hubs with authorization
app.MapHub<TicketInventoryHub>("/ticketInventoryHub").RequireAuthorization("CustomerOnly");
app.MapHub<OrderStatusHub>("/orderStatusHub").RequireAuthorization("CustomerOnly");

// Database initialization with security checks
if (!app.Environment.IsProduction())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<TicketBlasterDbContext>();
        try
        {
            await context.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error applying database migrations");
            throw;
        }
    }
}

Log.Information("TicketBlaster Server started successfully in {Environment} mode", app.Environment.EnvironmentName);

app.Run();

// Configuration classes
public partial class Program { } // Needed for integration tests

public class StripeSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}