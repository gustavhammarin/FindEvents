namespace API.Core;

public abstract record AppError(string Code, string Message, int StatusCode);

public sealed record NotFoundError(string Resource)
    : AppError($"{Resource}.NotFound", $"{Resource} hittades inte.", 404);

public sealed record ValidationError(string Code, string Message)
    : AppError(Code, Message, 400);

public sealed record ServiceUnavailableError(string Code, string Message)
    : AppError(Code, Message, 503);

public sealed record InternalError(string Code, string Message)
    : AppError(Code, Message, 500);
