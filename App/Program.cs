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
builder.Services.AddScoped<ImageCacheService>();
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
app.UseAdminBasicAuth(app.Configuration["Admin:Password"]);
app.UseStaticFiles();

app.MapFindEventsEndpoints();

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
