using System.Text;
using System.Xml;
using App.Configuration;
using App.Embedder;
using App.Persistence;
using App.Repositories;
using App.Scraper;
using App.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

app.UseStaticFiles();

app.MapGet("/", () => Results.Redirect("/evenemang", permanent: true));

app.MapGet("/robots.txt", (HttpContext ctx) =>
{
    var host = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
    return Results.Text(
        $"User-agent: *\nAllow: /\nDisallow: /evenemang?\n\nSitemap: {host}/sitemap.xml\n",
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

    var sb = new StringBuilder();
    var settings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = false, Async = true };
    await using var writer = XmlWriter.Create(sb, settings);

    await writer.WriteStartDocumentAsync();
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
