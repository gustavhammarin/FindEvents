using System.Text;
using System.Xml;
using App.Persistence;
using App.Repositories;
using App.Services;
using Microsoft.EntityFrameworkCore;

namespace App.Web;

public static class Endpoints
{
    public static WebApplication MapFindEventsEndpoints(this WebApplication app)
    {
        app.MapGet("/", () => Results.Redirect("/evenemang", permanent: true));

        app.MapGet("/healthz", async (AppDbContext db) =>
            await db.Database.CanConnectAsync() ? Results.Text("ok") : Results.StatusCode(503));

        app.MapGet("/img/{id:guid}", async (Guid id, IEventRepository repo, ImageCacheService cache, HttpContext ctx, CancellationToken ct) =>
        {
            var ev = await repo.GetEventByIdAsync(id, ct);
            if (ev?.ImageUrl is null) return Results.NotFound();

            var path = await cache.GetOrFetchAsync(ev.ImageUrl, ct);
            if (path is null) return Results.Redirect(ev.ImageUrl);

            // Serve only content we can positively identify as an image.
            var contentType = SniffImageType(path);
            if (contentType is null) return Results.NotFound();

            // Filename is a hash of the source URL — new URL means new path,
            // so the browser can cache the response forever.
            ctx.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            ctx.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(path, contentType);
        });

        app.MapGet("/robots.txt", (HttpContext ctx) =>
        {
            var host = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
            return Results.Text(
                $"User-agent: *\nAllow: /\nDisallow: /evenemang?\nDisallow: /admin\n\nSitemap: {host}/sitemap.xml\n",
                "text/plain");
        });

        app.MapGet("/sitemap.xml", async (AppDbContext db, HttpContext ctx) =>
        {
            var host = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var events = await db.Events
                .Where(e => e.StartDate >= today)
                .Select(e => new { e.Id, e.StartDate })
                .ToListAsync();

            // Write to StringBuilder (UTF-16 internally) but serve as UTF-8 — omit the
            // XML declaration so the document doesn't lie about its encoding.
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = false, Async = true };
            await using var writer = XmlWriter.Create(sb, settings);

            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            // Events list page
            writer.WriteStartElement("url");
            writer.WriteElementString("loc", $"{host}/evenemang");
            writer.WriteElementString("changefreq", "daily");
            writer.WriteElementString("priority", "0.8");
            writer.WriteEndElement();

            foreach (var ev in events)
            {
                writer.WriteStartElement("url");
                writer.WriteElementString("loc", $"{host}/evenemang/{ev.Id}");
                if (ev.StartDate.HasValue)
                    writer.WriteElementString("lastmod", ev.StartDate.Value.ToString("yyyy-MM-dd"));
                writer.WriteElementString("changefreq", "weekly");
                writer.WriteElementString("priority", "0.6");
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            await writer.FlushAsync();

            ctx.Response.ContentType = "application/xml; charset=utf-8";
            await ctx.Response.WriteAsync(sb.ToString());
        });

        return app;
    }

    private static string? SniffImageType(string path)
    {
        Span<byte> header = stackalloc byte[12];
        using var fs = File.OpenRead(path);
        var read = fs.Read(header);

        return header switch
        {
            [0xFF, 0xD8, ..] => "image/jpeg",
            [0x89, 0x50, 0x4E, 0x47, ..] => "image/png",
            [0x52, 0x49, 0x46, 0x46, _, _, _, _, 0x57, 0x45, 0x42, 0x50] when read >= 12 => "image/webp",
            [0x47, 0x49, 0x46, ..] => "image/gif",
            _ => null
        };
    }
}
