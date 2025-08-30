using System;

namespace API.Models;

public class EventSearchDoc
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Municipality { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

