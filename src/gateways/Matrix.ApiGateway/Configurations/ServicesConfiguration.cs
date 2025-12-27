using System.Text;
using Matrix.ApiGateway.Authorization.Jwt;
using Matrix.ApiGateway.DownstreamClients.CityCore;
using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Economy;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Account;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Assets;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Auth;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Sessions;
using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.ApiGateway.DownstreamClients.Population.Person;
using Matrix.BuildingBlocks.Api.Errors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Matrix.ApiGateway.Configurations
{
    public static class ServicesConfiguration
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            IServiceCollection services = builder.Services;
            ConfigurationManager configuration = builder.Configuration;

            services
               .AddPresentationLayer()
               .AddCorsLayer()
               .AddDownstreamClients(configuration)
               .AddSecurityLayer(configuration);
        }

        private static IServiceCollection AddPresentationLayer(this IServiceCollection services)
        {
            // При ModelState invalid, возвращаем кастомный ответ с ошибками валидации
            services.AddControllers()
               .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var errors = context.ModelState
                           .Where(kvp => kvp.Value?.Errors.Count > 0)
                           .ToDictionary(
                                keySelector: kvp => kvp.Key,
                                elementSelector: kvp => kvp.Value!.Errors
                                   .Select(e => e.ErrorMessage)
                                   .ToArray());

                        var error = new ErrorResponse(
                            Code: "Gateway.ValidationError",
                            Message: "Validation failed.",
                            Errors: errors,
                            TraceId: context.HttpContext.TraceIdentifier);

                        return new BadRequestObjectResult(error);
                    };
                });
            return services;
        }

        private static IServiceCollection AddCorsLayer(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(
                    name: "Frontend",
                    configurePolicy: policy =>
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
            services.AddHttpContextAccessor();
            services.AddTransient<ForwardClientInfoHeadersHandler>();
            services.AddTransient<ForwardAuthorizationHeaderHandler>();

            IConfigurationSection downstream = configuration.GetSection("DownstreamServices");

            services.AddHttpClient<ICityCoreApiClient, CityCoreApiClient>(client =>
                {
                    client.BaseAddress = new Uri(downstream["CityCore"]!);
                })
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            services.AddHttpClient<IEconomyApiClient, EconomyApiClient>(client =>
                {
                    client.BaseAddress = new Uri(downstream["Economy"]!);
                })
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            #region [ Population http clients ]

            string populationBaseUrl = downstream["Population"] ??
                                       throw new InvalidOperationException(
                                           "DownstreamServices:Population is not configured.");

            services.AddHttpClient<IPersonApiClient, PersonApiClient>(client =>
                {
                    client.BaseAddress = new Uri(populationBaseUrl);
                })
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            services.AddHttpClient<IPopulationApiClient, PopulationApiClient>(client =>
                {
                    client.BaseAddress = new Uri(populationBaseUrl);
                })
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            #endregion [ Population http clients ]

            #region [ Identity http clients ]

            // Identity downstream client
            // Общий baseUrl для Identity
            string identityBaseUrl = downstream["Identity"] ??
                                     throw new InvalidOperationException(
                                         "DownstreamServices:Identity is not configured.");

            // Identity Auth client (обычно анонимный, Authorization header не нужен, но client-info полезен)
            services.AddHttpClient<IIdentityAuthClient, IdentityAuthApiClient>(client =>
                {
                    client.BaseAddress = new Uri(identityBaseUrl);
                })
               .AddHttpMessageHandler<ForwardClientInfoHeadersHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            // Identity Account client
            services.AddHttpClient<IIdentityAccountClient, IdentityAccountApiClient>(client =>
                {
                    client.BaseAddress = new Uri(identityBaseUrl);
                })
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            // Static files (e.g. avatars)
            services.AddHttpClient<IIdentityAssetsClient, IdentityAssetsApiClient>(client =>
                {
                    client.BaseAddress = new Uri(identityBaseUrl);
                })
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            // Sessions (требует [Authorize], значит нужен forward Authorization)
            services.AddHttpClient<IIdentitySessionsClient, IdentitySessionsApiClient>(client =>
                {
                    client.BaseAddress = new Uri(identityBaseUrl);
                })
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .AddHttpMessageHandler<ForwardClientInfoHeadersHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            #endregion [ Identity http clients ]

            return services;
        }

        private static IServiceCollection AddSecurityLayer(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            IConfigurationSection jwtSection = configuration.GetSection("Jwt");
            JwtOptions jwtOptions = jwtSection.Get<JwtOptions>() ??
                                    throw new InvalidOperationException("Jwt configuration is missing.");

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

            return services;
        }

        private static void ConfigureTimeout(
            IServiceProvider sp,
            HttpClient client)
        {
            IHostEnvironment env = sp.GetRequiredService<IHostEnvironment>();

            client.Timeout = env.IsDevelopment()
                ? TimeSpan.FromMinutes(10)
                : TimeSpan.FromSeconds(20);
        }
    }
}
