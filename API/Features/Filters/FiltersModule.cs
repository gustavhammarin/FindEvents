using API.Extensions;
using API.Features.Filters;
using API.Modules;

namespace API.Features.Events;

public class FiltersModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<GetFiltersHandler>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/filters").WithTags("Filters");

        group.MapGet("/", async (
            GetFiltersHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(ct);
            return result.ToHttpResult();
        }).AllowAnonymous();
    }
}
