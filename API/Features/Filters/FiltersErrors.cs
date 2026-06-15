using API.Core;

namespace API.Features.Filters;

public static class FiltersErrors
{
    public static readonly AppError QueryFailed =
        new ServiceUnavailableError("Filters.QueryFailed", "Kunde inte hämta filter.");
}
