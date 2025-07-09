using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace TicketBlaster.Server.Security
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Add("Permissions-Policy", "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
            
            // Content Security Policy
            context.Response.Headers.Add("Content-Security-Policy", 
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://js.stripe.com; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: https:; " +
                "font-src 'self'; " +
                "connect-src 'self' https://api.stripe.com wss://localhost:* ws://localhost:*; " +
                "frame-src 'self' https://js.stripe.com https://hooks.stripe.com; " +
                "object-src 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self';"
            );

            // Remove server header
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            await _next(context);
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}