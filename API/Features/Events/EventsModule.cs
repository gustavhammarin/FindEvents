using API.Extensions;
using API.Modules;
using Microsoft.AspNetCore.Mvc;

namespace API.Features.Events;

public class EventsModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<GetEventsHandler>();
        services.AddScoped<GetEventByIdHandler>();
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
            return result.ToHttpResult();
        }).AllowAnonymous();

        group.MapGet("/{id}", async (
            [FromRoute] string id,
            GetEventByIdHandler handler,
            CancellationToken ct
        ) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.ToHttpResult();
        }).AllowAnonymous();
    }
}
