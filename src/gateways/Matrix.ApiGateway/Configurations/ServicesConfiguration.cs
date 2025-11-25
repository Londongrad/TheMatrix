using Matrix.ApiGateway.Authorization.Jwt;
using Matrix.ApiGateway.DownstreamClients.CityCore;
using Matrix.ApiGateway.DownstreamClients.Economy;
using Matrix.ApiGateway.DownstreamClients.Identity;
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
            services.AddHttpClient<IIdentityApiClient, IdentityApiClient>(client =>
            {
                client.BaseAddress = new Uri(downstream["Identity"]!);
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
