using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using UnitConverter.Common.Constants;
using UnitConverter.Common.Security;

namespace UnitConverter.UnitsDefinitions.Api.DependencyInjection;

public static class JwtAuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddUnitConverterJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var secret = configuration[ConfigurationKeys.Jwt.Secret]
            ?? throw new InvalidOperationException(JwtSecurityMessages.SecretRequired);
        if (secret.Length < JwtConfigurationDefaults.MinSecretLength)
        {
            throw new InvalidOperationException(
                string.Format(
                    JwtSecurityMessages.SecretMinLengthFormat,
                    JwtConfigurationDefaults.MinSecretLength));
        }

        var issuer = configuration[ConfigurationKeys.Jwt.Issuer] ?? JwtConfigurationDefaults.Issuer;
        var audience = configuration[ConfigurationKeys.Jwt.Audience] ?? JwtConfigurationDefaults.Audience;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.MasterDataCrud, policy =>
                policy.RequireRole(RoleNames.MasterDataCrud.ToArray()));

            options.AddPolicy(AuthorizationPolicies.MasterDataAdminDelete, policy =>
                policy.RequireRole(RoleNames.Admin));

            options.AddPolicy(AuthorizationPolicies.MasterDataAdminOverride, policy =>
                policy.RequireRole(RoleNames.Admin));

            options.AddPolicy(AuthorizationPolicies.MasterDataAdminApprove, policy =>
                policy.RequireRole(RoleNames.Admin));
        });

        return services;
    }
}
