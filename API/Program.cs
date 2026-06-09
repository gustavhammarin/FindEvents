using API.Configuration;
using API.Middleware;
using API.Services;
using Application.Activities.Core;
using Application.Events.Queries;
using Application.Interfaces;
using EventScraper.Interfaces;
using EventScraper.Utils;
using FluentValidation;
using Infrastructure.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(opt =>
{
    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    opt.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors();
builder.Services.AddSignalR();
builder.Services.AddMediatR(x =>
{
    x.RegisterServicesFromAssemblyContaining<GetEventList.Handler>();
    x.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddHttpClient<IHttpLoader, HttpLoader>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});

builder.Services.AddSingleton<SitemapService>();
builder.Services.AddScoped<IEventRepository, AppEventRepository>();
builder.Services.AddScoped<ScraperPipeline>();
builder.Services.AddHostedService<ScraperHostedService>();

var scraperAssembly = typeof(BaseScraper).Assembly;
foreach (var type in scraperAssembly.GetTypes()
    .Where(t => t.IsSubclassOf(typeof(BaseScraper)) && !t.IsAbstract
             && !(t.Namespace?.Contains("Tests") ?? false)))
{
    builder.Services.AddScoped(type);
}

builder.Services.AddTransient<ExceptionMiddleware>();
builder.Services.Configure<ElasticSettings>(
    builder.Configuration.GetSection("ElasticSettings"));
builder.Services.AddScoped<IElasticService, ElasticService>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod().AllowCredentials()
    .WithOrigins("http://localhost:3000", "https://localhost:3000",
                 "https://localhost:5173", "http://localhost:5173",
                 "http://127.0.0.1:5500", "https://127.0.0.1:5500"));

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGroup("api");

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

try
{
    var context = services.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
}
catch (Exception e)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(e, "An error occurred while migrating the database.");
}

app.Run();
