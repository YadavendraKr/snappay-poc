using System.Diagnostics;
using System.Text;

namespace OrderService.Middleware;

public class DaprRequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DaprRequestLoggingMiddleware> _logger;

    public DaprRequestLoggingMiddleware(RequestDelegate next, ILogger<DaprRequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        var isOrderPath = path.StartsWith("/api/orders", StringComparison.OrdinalIgnoreCase);
        var isSubOrderPath = path.StartsWith("/api/suborders", StringComparison.OrdinalIgnoreCase);

        if (!isOrderPath && !isSubOrderPath)
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var traceId = context.TraceIdentifier;
        var requestBody = string.Empty;
        var operationType = isOrderPath ? "Order" : "SubOrder";

        // Capture request body
        try
        {
            context.Request.EnableBuffering();
            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read request body for tracing");
        }

        var daprHeaders = ExtractDaprHeaders(context);

        _logger.LogInformation(
            "[DAPR] {OperationType} POST request started: Path={Path}, TraceId={TraceId}, RemoteIp={RemoteIp}, DaprRequestId={DaprRequestId}, Body={Body}",
            operationType,
            path,
            traceId,
            context.Connection.RemoteIpAddress,
            daprHeaders["dapr-request-id"],
            TruncateBody(requestBody));

        // Capture response body
        var originalBodyStream = context.Response.Body;
        var responseBody = string.Empty;

        try
        {
            using (var memoryStream = new MemoryStream())
            {
                context.Response.Body = memoryStream;

                await _next(context);

                try
                {
                    memoryStream.Position = 0;
                    using (var reader = new StreamReader(memoryStream, Encoding.UTF8))
                    {
                        responseBody = await reader.ReadToEndAsync();
                    }
                    memoryStream.Position = 0;
                    await memoryStream.CopyToAsync(originalBodyStream);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to capture response body");
                }
            }
        }
        finally
        {
            context.Response.Body = originalBodyStream;
            stopwatch.Stop();

            var statusCode = context.Response.StatusCode;
            var isSuccess = statusCode >= 200 && statusCode < 300;
            var logLevel = isSuccess ? LogLevel.Information : LogLevel.Warning;

            _logger.Log(
                logLevel,
                "[DAPR] {OperationType} POST request completed: Path={Path}, TraceId={TraceId}, StatusCode={StatusCode}, DurationMs={DurationMs}, DaprRequestId={DaprRequestId}, Response={Response}",
                operationType,
                path,
                traceId,
                statusCode,
                stopwatch.ElapsedMilliseconds,
                daprHeaders["dapr-request-id"],
                isSuccess ? TruncateBody(responseBody) : responseBody);
        }
    }

    private Dictionary<string, string> ExtractDaprHeaders(HttpContext context)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var daprHeaders = new[] { "dapr-request-id", "dapr-correlation-id", "dapr-tracing-id", "traceparent" };

        foreach (var header in daprHeaders)
        {
            if (context.Request.Headers.TryGetValue(header, out var value))
            {
                headers[header] = value.ToString();
            }
            else
            {
                headers[header] = "N/A";
            }
        }

        return headers;
    }

    private string TruncateBody(string body, int maxLength = 500)
    {
        if (string.IsNullOrEmpty(body)) return "[empty]";
        if (body.Length <= maxLength) return body;
        return body[..maxLength] + "...[truncated]";
    }
}
