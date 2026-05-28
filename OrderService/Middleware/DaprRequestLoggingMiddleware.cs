using System.Diagnostics;

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

        _logger.LogInformation(
            "Dapr POST request started: Path={Path}, TraceId={TraceId}, Method={Method}, RemoteIp={RemoteIp}",
            path,
            traceId,
            context.Request.Method,
            context.Connection.RemoteIpAddress);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "Dapr POST request completed: Path={Path}, TraceId={TraceId}, StatusCode={StatusCode}, DurationMs={DurationMs}",
                path,
                traceId,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
