using System.Text;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Matrix.BuildingBlocks.Api.Authorization
{
    public static class JwtAuthenticationExtensions
    {
        public static IServiceCollection AddJwtBearerAuthentication<TJwtOptions>(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName,
            bool requireHttpsMetadata = false,
            bool saveToken = false,
            Action<AuthenticationOptions>? configureAuthentication = null,
            Action<JwtBearerOptions>? configureJwtBearer = null)
            where TJwtOptions : class, IJwtValidationOptions
        {
            services.AddOptions<TJwtOptions>()
               .Bind(configuration.GetSection(sectionName))
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.Issuer),
                    failureMessage: $"{sectionName}:Issuer is required.")
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.Audience),
                    failureMessage: $"{sectionName}:Audience is required.")
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.SigningKey),
                    failureMessage: $"{sectionName}:SigningKey is required.")
               .ValidateOnStart();

            if (configureAuthentication is null)
            {
                services
                   .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                   .AddJwtBearer();
            }
            else
            {
                services
                   .AddAuthentication(configureAuthentication)
                   .AddJwtBearer();
            }

            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
               .Configure<IOptions<TJwtOptions>>((jwtBearerOptions, jwtOptions) =>
                {
                    TJwtOptions jwt = jwtOptions.Value;

                    jwtBearerOptions.RequireHttpsMetadata = requireHttpsMetadata;
                    jwtBearerOptions.SaveToken = saveToken;
                    jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwt.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };

                    configureJwtBearer?.Invoke(jwtBearerOptions);
                });

            return services;
        }
    }
}
