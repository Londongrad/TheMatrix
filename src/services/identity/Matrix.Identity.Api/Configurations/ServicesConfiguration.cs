using System.Text;
using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Identity.Application;
using Matrix.Identity.Infrastructure;
using Matrix.Identity.Infrastructure.Authentication.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Matrix.Identity.Api.Configurations
{
    public static class ServicesConfiguration
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            IServiceCollection services = builder.Services;
            ConfigurationManager configuration = builder.Configuration;

            services
               .AddPresentationLayer() // Controllers + Swagger
               .AddApplicationLayer() // MediatR, Application
               .AddInfrastructureLayer(configuration) // DbContext, репы, JwtAccessTokenService, PasswordHasher
               .AddSecurityLayer(configuration); // Authentication + Authorization
        }

        private static IServiceCollection AddPresentationLayer(this IServiceCollection services)
        {
            services.AddControllers();

            services.AddEndpointsApiExplorer();

            return services;
        }

        private static IServiceCollection AddApplicationLayer(this IServiceCollection services)
        {
            services.AddApplication();
            return services;
        }

        private static IServiceCollection AddInfrastructureLayer(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddInfrastructure(configuration);
            return services;
        }

        private static IServiceCollection AddSecurityLayer(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            IConfigurationSection jwtSection = configuration.GetSection("Jwt");
            JwtOptions jwtOptions = jwtSection.Get<JwtOptions>() ??
                                    throw new InvalidOperationException("Jwt configuration is missing.");

            services.AddOptions<IdentityInternalOptions>()
               .BindConfiguration("IdentityInternal")
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.ApiKey),
                    failureMessage: "IdentityInternal:ApiKey is required.")
               .ValidateOnStart();

            services
               .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
               .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false; // dev only
                    options.SaveToken = true;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                });

            services.AddAuthorization();

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();

            return services;
        }
    }
}
