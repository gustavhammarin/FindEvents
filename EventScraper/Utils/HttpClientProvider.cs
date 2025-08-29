using System;

namespace EventScraper.Utils;

public static class HttpClientProvider
{
    public static HttpClient Instance { get; }

    static HttpClientProvider()
    {
        Instance = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        Instance.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
    }
}
