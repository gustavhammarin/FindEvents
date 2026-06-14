using EventScraper.Interfaces;
using EventScraper.Sources;
using Microsoft.Extensions.DependencyInjection;

namespace EventScraper;

// All scraper sources. Comment out to disable, add new ones here.
public static class ScraperServiceExtensions
{
    public static IServiceCollection AddScraperSources(this IServiceCollection services)
    {
/*         services.AddScoped<IEventSource, JkpgSource>();
        services.AddScoped<IEventSource, HaboSource>();
        services.AddScoped<IEventSource, TranasSource>();
        services.AddScoped<IEventSource, VarnamoSource>();
        services.AddScoped<IEventSource, MullsjoSource>();
        services.AddScoped<IEventSource, GislavedSource>();
        services.AddScoped<IEventSource, AnebySource>();
        services.AddScoped<IEventSource, GnosjoSource>();
        services.AddScoped<IEventSource, VaggerydSource>();
        services.AddScoped<IEventSource, SavsjoSource>();
        services.AddScoped<IEventSource, VetlandaSource>();
        services.AddScoped<IEventSource, NassjoSource>();
        services.AddScoped<IEventSource, EksjoSource>(); */
        services.AddScoped<IEventSource, SvSource>();

        return services;
    }
}
