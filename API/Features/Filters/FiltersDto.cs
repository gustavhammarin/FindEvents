namespace API.Features.Filters;

public record FiltersDto(
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Municipalities,
    IReadOnlyList<string> Places);
