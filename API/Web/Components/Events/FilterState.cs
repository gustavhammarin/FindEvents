namespace API.Web.Components.Events;

public record FilterState(
    string Search = "",
    string Category = "",
    string Place = "",
    string Municipality = "",
    string Date = ""
);
