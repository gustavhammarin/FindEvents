using System;

namespace API.Configuration;

public class ElasticSettings
{
    public string Url { get; set; } = string.Empty;
    public string DefaultIndex { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
