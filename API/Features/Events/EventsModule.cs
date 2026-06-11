using API.Modules;
using Microsoft.AspNetCore.Mvc;

namespace API.Features.Events;

public class EventsModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<GetEventsHandler>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/events").WithTags("Events");

        group.MapGet("/", async (
            [AsParameters] GetEventsQuery query,
            GetEventsHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(query, ct);
            return Results.Ok(result);
        }).AllowAnonymous();
    }
}
