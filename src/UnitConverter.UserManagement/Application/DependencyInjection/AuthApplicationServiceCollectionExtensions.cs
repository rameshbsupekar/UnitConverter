using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UnitConverter.UserManagement.Application.Handlers;
using UnitConverter.UserManagement.Application.Services;
using UnitConverter.UserManagement.Application.Validators;
using UnitConverter.UserManagement.Common.Interfaces;
using UnitConverter.Common.Constants;
using UnitConverter.UserManagement.Common.Models;

namespace UnitConverter.UserManagement.Application.DependencyInjection;

public static class AuthApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddAuthApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(ConfigurationKeys.Jwt.Section));
        services.AddSingleton<ITokenGenerator>(sp =>
            new JwtTokenService(sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<JwtSettings>>().Value));

        services.AddScoped<IPasswordHasher, PasswordService>();
        services.AddScoped<IValidator<RegisterUserRequest>, RegisterUserValidator>();
        services.AddScoped<IValidator<LoginRequest>, LoginUserValidator>();
        services.AddScoped<IValidator<RefreshTokenRequest>, RefreshTokenValidator>();

        services.AddScoped<RegisterUserCommandHandler>();
        services.AddScoped<LoginUserCommandHandler>();
        services.AddScoped<RefreshTokenCommandHandler>();

        return services;
    }
}
