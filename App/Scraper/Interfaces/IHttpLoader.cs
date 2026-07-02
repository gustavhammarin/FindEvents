using System.IO.Compression;
using System.Xml.Linq;
using HtmlAgilityPack;

namespace App.Scraper.Interfaces;

public interface IHttpLoader
{
    Task<XDocument> LoadXmlAsync(string url, bool isGz);
    Task<HtmlAgilityPack.HtmlDocument?> LoadHtmlAsync(string url);
    Task<string> GetStringAsync(string url);
}

public class HttpLoader : IHttpLoader
{
    private readonly HttpClient _client;
    private const int MaxAttempts = 3;

    public HttpLoader(HttpClient client)
    {
        _client = client;
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
        _client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        _client.DefaultRequestHeaders.Add("Referer", "https://jkpg.com/evenemang");
        _client.DefaultRequestHeaders.Add("Origin", "https://jkpg.com");
    }

    public async Task<XDocument> LoadXmlAsync(string url, bool isGz)
    {
        using var resp = await GetWithRetryAsync(url) ?? throw new HttpRequestException($"Failed to fetch {url} after {MaxAttempts} attempts");
        await using var stream = await resp.Content.ReadAsStreamAsync();
        var xmlStream = isGz
            ? new GZipStream(stream, CompressionMode.Decompress)
            : stream;
        return await XDocument.LoadAsync(xmlStream, LoadOptions.None, CancellationToken.None);
    }

    public async Task<HtmlDocument?> LoadHtmlAsync(string url)
    {
        using var resp = await GetWithRetryAsync(url);
        if (resp is null) return null;
        var html = await resp.Content.ReadAsStringAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc;
    }

    public async Task<string> GetStringAsync(string url)
    {
        using var resp = await GetWithRetryAsync(url);
        return resp is null ? "" : await resp.Content.ReadAsStringAsync();
    }

    /// <summary>GET with retry on 5xx/429, network errors and timeouts. Returns null when all attempts fail.</summary>
    private async Task<HttpResponseMessage?> GetWithRetryAsync(string url)
    {
        var backoff = TimeSpan.FromSeconds(1);
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var resp = await _client.GetAsync(url);
                if (resp.IsSuccessStatusCode) return resp;

                var status = (int)resp.StatusCode;
                resp.Dispose();
                if (status != 429 && status < 500) return null;
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
                // HttpClient timeout
            }

            if (attempt < MaxAttempts)
            {
                await Task.Delay(backoff);
                backoff *= 3;
            }
        }
        return null;
    }
}
