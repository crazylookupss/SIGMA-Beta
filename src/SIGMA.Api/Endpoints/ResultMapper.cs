using SIGMA.Domain.Common;

namespace SIGMA.Api.Endpoints;

internal static class ResultMapper
{
    public static IResult ToResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return result.Value is null
                ? Results.NotFound(new { title = "Not Found", status = 404 })
                : Results.Ok(new { data = result.Value });

        return result.Error!.Type switch
        {
            ErrorType.NotFound => Results.NotFound(new
            {
                type = "https://tools.ietf.org/html/rfc9457",
                title = result.Error.Code,
                status = 404,
                detail = result.Error.Description,
            }),
            ErrorType.Validation => Results.BadRequest(new
            {
                type = "https://tools.ietf.org/html/rfc9457",
                title = result.Error.Code,
                status = 400,
                detail = result.Error.Description,
            }),
            ErrorType.ExternalService => Results.StatusCode(502),
            _ => Results.Problem(
                title: result.Error.Code,
                detail: result.Error.Description,
                statusCode: 500),
        };
    }
}
