using EventScraper.Configuration;
using EventScraper.Extractors;
using EventScraper.Interfaces;
using EventScraper.Sources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventScraper;

public static class ScraperServiceExtensions
{
    public static IServiceCollection AddScraperServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LlmSettings>(configuration.GetSection("LlmSettings"));
        services.Configure<MistralSettings>(configuration.GetSection("MistralSettings"));

        services.AddHttpClient<IHttpLoader, HttpLoader>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        var mistralApiKey = configuration["MistralSettings:ApiKey"];
        if (!string.IsNullOrWhiteSpace(mistralApiKey))
        {
            services.AddHttpClient<ILlmExtractor, MistralExtractor>(client =>
            {
                client.BaseAddress = new Uri(configuration["MistralSettings:BaseUrl"] ?? "https://api.mistral.ai/v1");
                client.Timeout = TimeSpan.FromSeconds(60);
            });
        }
        else
        {
            services.AddHttpClient<ILlmExtractor, MlxExtractor>(client =>
            {
                client.BaseAddress = new Uri(configuration["LlmSettings:BaseUrl"] ?? "http://127.0.0.1:8000/v1");
                client.Timeout = TimeSpan.FromSeconds(120);
            });
        }

        services.AddScoped<ScraperPipeline>();
        services.AddHostedService<ScraperHostedService>();

        /*services.AddScoped<IEventSource, JkpgSource>();
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
        services.AddScoped<IEventSource, EksjoSource>();
        services.AddScoped<IEventSource, SvSource>(); */ 

        return services;
    }
}
