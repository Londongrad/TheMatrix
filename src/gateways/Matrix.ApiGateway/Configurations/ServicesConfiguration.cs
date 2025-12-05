using Matrix.ApiGateway.Authorization.Jwt;
using Matrix.ApiGateway.DownstreamClients.CityCore;
using Matrix.ApiGateway.DownstreamClients.Economy;
using Matrix.ApiGateway.DownstreamClients.Identity.Account;
using Matrix.ApiGateway.DownstreamClients.Identity.Auth;
using Matrix.ApiGateway.DownstreamClients.Population;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Matrix.ApiGateway.Configurations
{
    public static class ServicesConfiguration
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            var services = builder.Services;
            var configuration = builder.Configuration;

            services
                .AddPresentationLayer()
                .AddCorsLayer()
                .AddDownstreamClients(configuration)
                .AddSecurityLayer(configuration);
        }

        private static IServiceCollection AddPresentationLayer(this IServiceCollection services)
        {
            services.AddControllers();
            return services;
        }

        private static IServiceCollection AddCorsLayer(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy =>
                {
                    policy
                        .WithOrigins("https://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            return services;
        }
        private static IServiceCollection AddDownstreamClients(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var downstream = configuration.GetSection("DownstreamServices");

            services.AddHttpClient<ICityCoreApiClient, CityCoreApiClient>(client =>
            {
                client.BaseAddress = new Uri(downstream["CityCore"]!);
            });

            services.AddHttpClient<IEconomyApiClient, EconomyApiClient>(client =>
            {
                client.BaseAddress = new Uri(downstream["Economy"]!);
            });

            services.AddHttpClient<IPopulationApiClient, PopulationApiClient>(client =>
            {
                client.BaseAddress = new Uri(downstream["Population"]!);
            });

            // Identity downstream client
            // Общий baseUrl для Identity
            var identityBaseUrl = downstream["Identity"]
                ?? throw new InvalidOperationException("DownstreamServices:Identity is not configured.");

            // Identity Auth client
            services.AddHttpClient<IIdentityAuthClient, IdentityAuthApiClient>(client =>
            {
                client.BaseAddress = new Uri(identityBaseUrl);
            });

            // Identity Account client
            services.AddHttpClient<IIdentityAccountClient, IdentityAccountApiClient>(client =>
            {
                client.BaseAddress = new Uri(identityBaseUrl);
            });

            return services;
        }

        private static IServiceCollection AddSecurityLayer(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection("Jwt");
            var jwtOptions = jwtSection.Get<JwtOptions>()
                             ?? throw new InvalidOperationException("Jwt configuration is missing.");

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
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                });

            services.AddAuthorization();

            return services;
        }
    }
}
