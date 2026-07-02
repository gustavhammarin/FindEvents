using System.Text;
using System.Xml;
using App.Configuration;
using App.Embedder;
using App.Persistence;
using App.Repositories;
using App.Scraper;
using App.Services;
using App.Web;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

// Single .env file (repo root) holds all local config — loaded into env vars
// before the config system reads them. No-op when the file doesn't exist.
DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Behind Caddy/nginx: trust X-Forwarded-Proto/Host so canonical URLs and
// sitemap get https + the real domain.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseVector()));

builder.Services.AddFindEventsConfiguration(builder.Configuration);

builder.Services.AddScoped<IEventRepository, AppEventRepository>();
builder.Services.AddScraperServices(builder.Configuration);

builder.Services.AddHttpClient<MistralEmbeddingService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["MistralSettings:BaseUrl"] ?? "https://api.mistral.ai/");
    var apiKey = builder.Configuration["MistralSettings:ApiKey"];
    if (!string.IsNullOrWhiteSpace(apiKey))
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
});

builder.Services.AddSingleton<CategoryClassifierService>();
builder.Services.AddSingleton<EventEmbeddingService>();

builder.Services.AddScoped<EventService>();

builder.Services.AddRazorPages(options => options.RootDirectory = "/Web/Pages");

var app = builder.Build();

app.UseForwardedHeaders();
app.UseStaticFiles();
app.UseAdminBasicAuth(app.Configuration["Admin:Password"]);

app.MapGet("/", () => Results.Redirect("/evenemang", permanent: true));

app.MapGet("/healthz", async (AppDbContext db) =>
    await db.Database.CanConnectAsync() ? Results.Text("ok") : Results.StatusCode(503));

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

app.MapRazorPages();

using var scope = app.Services.CreateScope();
try
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
}
catch (Exception e)
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogError(e, "Migration failed");
}

app.Run();
