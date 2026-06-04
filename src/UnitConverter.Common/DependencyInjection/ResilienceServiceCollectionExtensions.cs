using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using UnitConverter.Common.Contracts.Resilience;
using UnitConverter.Common.Resilience;

namespace UnitConverter.Common.DependencyInjection;

public static class ResilienceServiceCollectionExtensions
{
    public static IServiceCollection AddSharedResilience(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var configurer = new ResilienceConfigurer();
        configurer.AddRateLimiting(services, configuration);
        services.AddSingleton<IResilienceConfigurer>(configurer);
        return services;
    }

    public static IHttpClientBuilder AddSharedHttpResilience(
        this IHttpClientBuilder builder,
        int maxRetryAttempts = ResilienceDefaults.HttpMaxRetryAttempts)
    {
        new ResilienceConfigurer().AddHttpResilience(builder, maxRetryAttempts);
        return builder;
    }
}
