using System.Text;
using KeyLoyalty.Infrastructure.Services;

namespace KeyLoyalty.API.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly ILoyaltyLogService _logService;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger, ILoyaltyLogService logService)
    {
        _next = next;
        _logger = logger;
        _logService = logService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = correlationId;
        
        await LogInboundRequest(context, correlationId);
        
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        await LogOutboundResponse(context, responseBody, originalBodyStream, correlationId);
    }

    private async Task LogInboundRequest(HttpContext context, string correlationId)
    {
        context.Request.EnableBuffering();
        var body = await ReadStreamAsync(context.Request.Body);
        context.Request.Body.Position = 0;

        var headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
        
        await _logService.LogInboundRequestAsync(
            context.Request.Method,
            context.Request.Path,
            headers,
            body,
            correlationId);

        _logger.LogInformation("INBOUND: {Method} {Path} | CorrelationId: {CorrelationId} | Headers: {@Headers} | Body: {Body}",
            context.Request.Method,
            context.Request.Path,
            correlationId,
            headers,
            body);
    }

    private async Task LogOutboundResponse(HttpContext context, MemoryStream responseBody, Stream originalBodyStream, string correlationId)
    {
        responseBody.Seek(0, SeekOrigin.Begin);
        var response = await ReadStreamAsync(responseBody);
        responseBody.Seek(0, SeekOrigin.Begin);

        var headers = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
        
        await _logService.LogOutboundResponseAsync(
            context.Response.StatusCode,
            headers,
            response,
            correlationId);

        _logger.LogInformation("OUTBOUND: {StatusCode} | CorrelationId: {CorrelationId} | Headers: {@Headers} | Body: {Body}",
            context.Response.StatusCode,
            correlationId,
            headers,
            response);

        await responseBody.CopyToAsync(originalBodyStream);
    }

    private static async Task<string> ReadStreamAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}