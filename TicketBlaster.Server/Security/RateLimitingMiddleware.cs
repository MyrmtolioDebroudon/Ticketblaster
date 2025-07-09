using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace TicketBlaster.Server.Security
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly RateLimitOptions _options;

        public RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache, RateLimitOptions options)
        {
            _next = next;
            _cache = cache;
            _options = options;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            var rateLimitAttribute = endpoint?.Metadata.GetMetadata<RateLimitAttribute>();
            
            if (rateLimitAttribute != null || _options.EnableGlobalRateLimit)
            {
                var key = GenerateClientKey(context);
                var rateLimitCounter = await _cache.GetOrCreateAsync(key, async entry =>
                {
                    entry.AbsoluteExpiration = DateTime.UtcNow.AddSeconds(_options.Window);
                    return new RateLimitCounter
                    {
                        Timestamp = DateTime.UtcNow,
                        Count = 0
                    };
                });

                rateLimitCounter.Count++;

                var limit = rateLimitAttribute?.Limit ?? _options.Limit;
                
                if (rateLimitCounter.Count > limit)
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                    return;
                }

                // Update the cache
                _cache.Set(key, rateLimitCounter, TimeSpan.FromSeconds(_options.Window));
            }

            await _next(context);
        }

        private static string GenerateClientKey(HttpContext context)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var path = context.Request.Path.ToString().ToLowerInvariant();
            
            // For authenticated users, use user ID instead of IP
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst("id")?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    return $"rate_limit_{userId}_{path}";
                }
            }
            
            return $"rate_limit_{clientIp}_{path}";
        }
    }

    public class RateLimitCounter
    {
        public DateTime Timestamp { get; set; }
        public int Count { get; set; }
    }

    public class RateLimitOptions
    {
        public int Limit { get; set; } = 100;
        public int Window { get; set; } = 60; // seconds
        public bool EnableGlobalRateLimit { get; set; } = true;
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RateLimitAttribute : Attribute
    {
        public int Limit { get; set; }
        public int Window { get; set; } = 60; // seconds

        public RateLimitAttribute(int limit)
        {
            Limit = limit;
        }
    }

    public static class RateLimitingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder, Action<RateLimitOptions> configureOptions = null)
        {
            var options = new RateLimitOptions();
            configureOptions?.Invoke(options);
            
            return builder.UseMiddleware<RateLimitingMiddleware>(options);
        }
    }
}