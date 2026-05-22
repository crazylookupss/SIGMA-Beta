using System.Net;
using SIGMA.Domain.Common;

namespace SIGMA.Api.Middleware;

internal sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, error) = exception switch
        {
            _ => (
                HttpStatusCode.InternalServerError,
                Error.Failure("InternalServerError", "An unexpected error occurred. Please try again later.")
            ),
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            type = "https://tools.ietf.org/html/rfc9457",
            title = error.Code,
            status = (int)statusCode,
            detail = error.Description,
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
