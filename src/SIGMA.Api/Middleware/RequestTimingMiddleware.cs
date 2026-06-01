using System.Diagnostics;

namespace SIGMA.Api.Middleware;

internal sealed class RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? "/";

        try
        {
            await next(context);
        }
        finally
        {
            sw.Stop();
            var elapsed = sw.ElapsedMilliseconds;
            var method = context.Request.Method;
            var status = context.Response.StatusCode;

            if (elapsed > 2000)
            {
                logger.LogWarning("Slow request: {Method} {Path} responded {Status} in {Elapsed}ms",
                    method, path, status, elapsed);
            }
            else
            {
                logger.LogInformation("Request: {Method} {Path} responded {Status} in {Elapsed}ms",
                    method, path, status, elapsed);
            }
        }
    }
}
