namespace App.Services;

public record EventsFilter
{
    public string? Search { get; init; }
    public List<string> Categories { get; init; } = [];
    public List<string> Places { get; init; } = [];
    public string? StartDate { get; init; }
    public string? CursorStartDate { get; init; }
    public string? CursorId { get; init; }
    public int PageSize { get; init; } = 32;
}
