using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace App.Configuration;

public static class ConfigurationServiceExtensions
{
    public static IServiceCollection AddFindEventsConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LlmSettings>(configuration.GetSection("LlmSettings"));
        services.Configure<MistralSettings>(configuration.GetSection("MistralSettings"));
        services.Configure<ImageCacheSettings>(configuration.GetSection("ImageCacheSettings"));
        services.AddSingleton<MistralRateLimiter>();
        return services;
    }
}
