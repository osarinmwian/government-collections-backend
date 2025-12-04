using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace OmniChannel.Middleware
{
    public class ApiKeyAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public ApiKeyAuthMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only apply to notification endpoints
            if (context.Request.Path.StartsWithSegments("/api/notifications"))
            {
                var apiKey = _configuration["NotificationService:ApiKey"];
                
                if (!string.IsNullOrEmpty(apiKey))
                {
                    if (!context.Request.Headers.TryGetValue("X-API-Key", out var extractedApiKey))
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("API Key missing");
                        return;
                    }

                    if (!apiKey.Equals(extractedApiKey))
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Invalid API Key");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }

    // Extension method for easy registration
    public static class ApiKeyAuthMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiKeyAuthMiddleware>();
        }
    }
}

// Add to your Program.cs
// app.UseApiKeyAuth(); // Add this line before app.MapControllers();