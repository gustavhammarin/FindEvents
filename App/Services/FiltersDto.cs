namespace App.Services;

public record FiltersDto(
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Places);
