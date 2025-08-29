using System;
using System.IO.Compression;
using System.Xml.Linq;
using HtmlAgilityPack;

namespace EventScraper.Interfaces;

public interface IHttpLoader
{
    Task<XDocument> LoadXmlAsync(string url, bool isGz);
    Task<HtmlAgilityPack.HtmlDocument?> LoadHtmlAsync(string url);
    Task<string> GetStringAsync(string url);
}

public class HttpLoader : IHttpLoader
{
    private readonly HttpClient _client;

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
        await using var stream = await _client.GetStreamAsync(url);
        var xmlStream = isGz
            ? new GZipStream(stream, CompressionMode.Decompress)
            : stream;
        return XDocument.Load(xmlStream);
    }

    public async Task<HtmlDocument?> LoadHtmlAsync(string url)
    {
        try
        {
            var resp = await _client.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return null;
            var html = await resp.Content.ReadAsStringAsync();
            var doc  = new HtmlDocument();
            doc.LoadHtml(html);
            return doc;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<string> GetStringAsync(string url)
    {
        return await _client.GetStringAsync(url);
    }
}
