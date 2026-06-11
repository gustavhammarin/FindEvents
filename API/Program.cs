using API.Configuration;
using API.Middleware;
using API.Modules;
using API.Services;
using Application.Interfaces;
using EventScraper.Configuration;
using EventScraper.Extractors;
using EventScraper.Interfaces;
using EventScraper.Utils;
using Infrastructure.Events;
using Microsoft.EntityFrameworkCore;
using Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors();

builder.Services.AddHttpClient<IHttpLoader, HttpLoader>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});

builder.Services.Configure<LlmSettings>(
    builder.Configuration.GetSection("LlmSettings"));

builder.Services.AddHttpClient<ILlmExtractor, MlxExtractor>(client =>
{
    var baseUrl = builder.Configuration["LlmSettings:BaseUrl"] ?? "http://127.0.0.1:8000/v1";
    client.Timeout = TimeSpan.FromSeconds(120);
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddScoped<IEventRepository, AppEventRepository>();
builder.Services.AddScoped<ScraperPipeline>();
builder.Services.AddHostedService<ScraperHostedService>();

// Auto-register all IEventSource implementations
var scraperAssembly = typeof(IEventSource).Assembly;
foreach (var type in scraperAssembly.GetTypes()
    .Where(t => typeof(IEventSource).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract))
{
    builder.Services.AddScoped(typeof(IEventSource), type);
}

builder.Services.Configure<ElasticSettings>(
    builder.Configuration.GetSection("ElasticSettings"));
builder.Services.AddScoped<IElasticService, ElasticService>();

// Auto-discover and register all IModule implementations
var modules = typeof(Program).Assembly.GetTypes()
    .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
    .Select(t => (IModule)Activator.CreateInstance(t)!)
    .ToList();

foreach (var module in modules)
    module.RegisterServices(builder.Services, builder.Configuration);

builder.Services.AddTransient<ExceptionMiddleware>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod().AllowCredentials()
    .WithOrigins("http://localhost:3000", "https://localhost:3000",
                 "https://localhost:5173", "http://localhost:5173",
                 "http://127.0.0.1:5500", "https://127.0.0.1:5500"));

app.MapControllers();

foreach (var module in modules)
    module.MapEndpoints(app);

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
