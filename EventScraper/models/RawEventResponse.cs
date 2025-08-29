using System;
using Newtonsoft.Json;

namespace EventScraper.models;

// RawEventResponse.cs
public class RawEventResponse
{
    [JsonProperty("event")]
    public RawEvent Event { get; set; } = null!;
}

public class RawEvent
{
    [JsonProperty("name")]
    public LocalizedString Name { get; set; } = null!;

    [JsonProperty("dates")]
    public List<RawDate> Dates { get; set; } = new();

    [JsonProperty("place")]
    public RawPlace Place { get; set; } = null!;

    [JsonProperty("description")]
    public LocalizedString Description { get; set; } = new();

    [JsonProperty("images")]
    public List<RawImage> Images { get; set; } = new();
}

public class LocalizedString
{
    [JsonProperty("sv")]
    public string Sv { get; set; } = null!;
}

public class RawDate
{
    [JsonProperty("startDate")]
    public DateTime StartDate { get; set; }

    [JsonProperty("endDate")]
    public DateTime EndDate { get; set; }

    [JsonProperty("startTime")]
    public string StartTime { get; set; } = null!;

    [JsonProperty("endTime")]
    public string EndTime { get; set; } = null!;
}

public class RawPlace
{
    [JsonProperty("name")]
    public LocalizedString Name { get; set; } = null!;
}

public class RawImage
{
    [JsonProperty("url")]
    public string Url { get; set; } = null!;
}