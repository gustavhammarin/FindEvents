namespace App.Web.Components.Events;

public record FilterState(
    string Search = "",
    string Category = "",
    string Place = "",
    string Date = ""
);
