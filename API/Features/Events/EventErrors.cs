using API.Core;

namespace API.Features.Events;

public static class EventErrors
{
    public static readonly AppError QueryFailed =
        new InternalError("Event.QueryFailed", "Kunde inte hämta evenemang.");
}
