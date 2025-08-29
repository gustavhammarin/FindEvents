using System;
using EventScraper.Interfaces;

namespace EventScraper.Utils;

public class SitemapService
{
    private readonly IHttpLoader _loader;

    public SitemapService(IHttpLoader loader) => _loader = loader;

    public async Task<IEnumerable<string>> GetEventPageUrlsAsync(
        string sitemapIndexUrl,
        Func<string, bool> urlFilter)
    {
        var indexDoc = await _loader.LoadXmlAsync(sitemapIndexUrl, isGz: false);
        var ns       = indexDoc.Root!.GetDefaultNamespace();

        // Hämta alla sub-sitemap-URLs
        var sitemapUrls = indexDoc
            .Root
            .Elements(ns + "sitemap")
            .Select(x => x.Element(ns + "loc")!.Value);

        var allPages = new List<string>();

        foreach (var smUrl in sitemapUrls)
        {
            var isGz  = smUrl.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
            var smDoc = await _loader.LoadXmlAsync(smUrl, isGz);
            var smNs  = smDoc.Root!.GetDefaultNamespace();

            var pages = smDoc
                .Root
                .Elements(smNs + "url")
                .Select(x => x.Element(smNs + "loc")!.Value)
                .Where(urlFilter);

            allPages.AddRange(pages);
        }

        return allPages;
    }
}
