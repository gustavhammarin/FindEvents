using API.Core;
using EventScraper.Categorization;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace API.Features.Filters;

public class GetFiltersHandler(AppDbContext db, ILogger<GetFiltersHandler> logger)
{
    public async Task<Result<FiltersDto>> HandleAsync(CancellationToken ct)
    {
        try
        {
            var municipalities = await db.Events
                .AsNoTracking()
                .Select(e => e.Municipality)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync(ct);

            if (municipalities.Count == 0)
                return Result<FiltersDto>.Failure(FiltersErrors.QueryFailed);

            var platser = await db.Events
                .AsNoTracking()
                .Where(e => e.Place != null && e.Place != "")
                .Select(e => e.Place!)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync(ct);

            return Result<FiltersDto>.Success(
                new FiltersDto(EventCategories.Categories, municipalities, platser));
        }
        catch
        {
            logger.LogWarning("Failed to get filters");
            return Result<FiltersDto>.Failure(FiltersErrors.QueryFailed);
        }
    }
}
