using System.Text;

namespace GovernmentCollections.API.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = correlationId;

        // Log request
        await LogRequest(context, correlationId);

        // Capture response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        // Log response
        await LogResponse(context, correlationId);

        // Copy response back to original stream
        await responseBody.CopyToAsync(originalBodyStream);
    }

    private async Task LogRequest(HttpContext context, string correlationId)
    {
        try
        {
            var request = context.Request;
            var requestBody = string.Empty;

            if (request.ContentLength > 0 && request.Body.CanRead)
            {
                request.EnableBuffering();
                var buffer = new byte[Convert.ToInt32(request.ContentLength)];
                await request.Body.ReadAsync(buffer, 0, buffer.Length);
                requestBody = Encoding.UTF8.GetString(buffer);
                request.Body.Position = 0;
            }

            _logger.LogInformation(
                "Request {CorrelationId}: {Method} {Path} {QueryString} - Body: {RequestBody}",
                correlationId,
                request.Method,
                request.Path,
                request.QueryString,
                requestBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging request for correlation ID: {CorrelationId}", correlationId);
        }
    }

    private async Task LogResponse(HttpContext context, string correlationId)
    {
        try
        {
            var response = context.Response;
            response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            _logger.LogInformation(
                "Response {CorrelationId}: {StatusCode} - Body: {ResponseBody}",
                correlationId,
                response.StatusCode,
                responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging response for correlation ID: {CorrelationId}", correlationId);
        }
    }
}