using API.Core;
using Persistence;

namespace API.Features.Events;

public class GetEventByIdHandler(AppDbContext db)
{
    public async Task<Result<EventDto>> HandleAsync(string id, CancellationToken ct)
    {
        var ev = await db.Events.FindAsync(id, ct);
        if (ev is null)
            return Result<EventDto>.Failure(new NotFoundError("Event"));

        return Result<EventDto>.Success(EventDto.FromEntity(ev));
    }
}
