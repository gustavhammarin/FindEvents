using App.Services;
using App.Web;
using EventScraper;
using EventScraper.Interfaces;
using Microsoft.EntityFrameworkCore;
using Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IEventRepository, AppEventRepository>();
builder.Services.AddScraperServices(builder.Configuration);
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<PopupManager>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<Shell>()
    .AddInteractiveServerRenderMode();

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
