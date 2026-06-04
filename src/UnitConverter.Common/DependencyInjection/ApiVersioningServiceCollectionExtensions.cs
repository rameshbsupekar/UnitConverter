using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace UnitConverter.Common.DependencyInjection;

/// <summary>
/// URL-segment API versioning shared by conversion and auth hosts.
/// </summary>
public static class ApiVersioningServiceCollectionExtensions
{
    public static IServiceCollection AddUrlSegmentApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
