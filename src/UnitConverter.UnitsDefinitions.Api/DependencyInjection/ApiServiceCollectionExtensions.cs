using FluentValidation;
using UnitConverter.UnitsDefinitions.Api.Security;
using UnitConverter.UnitsDefinitions.Api.Services;
using UnitConverter.UnitsDefinitions.Api.Validators;
using UnitConverter.UnitsDefinitions.Contracts.Requests;
using UnitConverter.UnitsDefinitions.Conversion;

namespace UnitConverter.UnitsDefinitions.Api.DependencyInjection;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddUnitConverterApiServices(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateUnitRequest>, CreateUnitRequestValidator>();
        services.AddScoped<IValidator<UpdateUnitRequest>, UpdateUnitRequestValidator>();
        services.AddScoped<IValidator<RejectUnitRequest>, RejectUnitRequestValidator>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();
        services.AddSingleton<IConversionEngine, ConversionEngine>();
        services.AddScoped<IConvertUnitsHandler, ConvertUnitsHandler>();
        services.AddScoped<ICatalogQueries, CatalogQueries>();
        services.AddScoped<IUnitAdminService, UnitAdminService>();
        return services;
    }
}
