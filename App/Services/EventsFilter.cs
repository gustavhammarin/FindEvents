namespace App.Services;

public record EventsFilter
{
    public string? Search { get; init; }
    public string? Category { get; init; }
    public string? Place { get; init; }
public string? StartDate { get; init; }
    public string? CursorStartDate { get; init; }
    public string? CursorId { get; init; }
    public int PageSize { get; init; } = 32;
}
