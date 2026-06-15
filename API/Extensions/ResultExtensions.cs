using API.Core;

namespace API.Extensions;

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
            return onSuccess?.Invoke(result.Value!) ?? Results.Ok(result.Value);

        var error = result.Error!;
        return Results.Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: error.StatusCode
        );
    }
}
