using System.Text;
using MassTransit;
using Matrix.ApiGateway.Authorization.Jwt;
using Matrix.ApiGateway.Authorization.PermissionsVersion;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Abstractions;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Options;
using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.Consumers;
using Matrix.ApiGateway.DownstreamClients.CityCore;
using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Economy;
using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Permissions;
using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Roles;
using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Users;
using Matrix.ApiGateway.DownstreamClients.Identity.Internal.PermissionsVersion;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Account;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Assets;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Auth;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Sessions;
using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.ApiGateway.DownstreamClients.Population.Person;
using Matrix.BuildingBlocks.Api.Errors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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
               .AddSecurityLayer(configuration)
               .AddPermissionsVersionServices(configuration);
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

            #region [ Self ]

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

            #endregion [ Self ]

            #region [ Admin ]

            services.AddHttpClient<IIdentityAdminRolesClient, IdentityAdminRolesApiClient>(client =>
                {
                    client.BaseAddress = new Uri(identityBaseUrl);
                })
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            services.AddHttpClient<IIdentityAdminPermissionsClient, IdentityAdminPermissionsApiClient>(client =>
                {
                    client.BaseAddress = new Uri(identityBaseUrl);
                })
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            services.AddHttpClient<IIdentityAdminUsersClient, IdentityAdminUsersApiClient>(client =>
                {
                    client.BaseAddress = new Uri(identityBaseUrl);
                })
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            #endregion [ Admin ]

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

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = PermissionsVersionJwtEvents.HandleTokenValidated,
                        OnChallenge = PermissionsVersionJwtEvents.HandleChallenge
                    };
                });

            services.AddAuthorization();

            return services;
        }

        private static IServiceCollection AddPermissionsVersionServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<IdentityInternalOptions>()
               .Bind(configuration.GetSection(IdentityInternalOptions.SectionName))
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.BaseUrl),
                    failureMessage: "IdentityInternal:BaseUrl is required.")
               .Validate(
                    validation: o => Uri.TryCreate(
                        uriString: o.BaseUrl,
                        uriKind: UriKind.Absolute,
                        result: out _),
                    failureMessage: "IdentityInternal:BaseUrl must be an absolute URI.")
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.ApiKey),
                    failureMessage: "IdentityInternal:ApiKey is required.")
               .ValidateOnStart();

            services.AddOptions<PermissionsVersionOptions>()
               .Bind(configuration.GetSection(PermissionsVersionOptions.SectionName))
               .Validate(
                    validation: o => o.CacheTtlSeconds > 0,
                    failureMessage: "PermissionsVersion:CacheTtlSeconds must be greater than 0.")
               .ValidateOnStart();

            services.AddOptions<RabbitMqOptions>()
               .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.Host),
                    failureMessage: "RabbitMq:Host is required.")
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.Username),
                    failureMessage: "RabbitMq:Username is required.")
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.Password),
                    failureMessage: "RabbitMq:Password is required.")
               .ValidateOnStart();

            services.AddStackExchangeRedisCache(o =>
            {
                o.Configuration = configuration["Redis:ConnectionString"];
                o.InstanceName = configuration["Redis:InstanceName"];
            });

            services.AddHttpClient<IIdentityPermissionsVersionClient, IdentityPermissionsVersionClient>((
                sp,
                client) =>
            {
                IdentityInternalOptions options = sp.GetRequiredService<IOptions<IdentityInternalOptions>>()
                   .Value;

                client.BaseAddress = new Uri(
                    uriString: options.BaseUrl,
                    uriKind: UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(3);

                client.DefaultRequestHeaders.Remove("X-Internal-Key");
                client.DefaultRequestHeaders.Add(
                    name: "X-Internal-Key",
                    value: options.ApiKey);
            });

            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.AddConsumer<UserSecurityStateChangedConsumer>();

                x.UsingRabbitMq((
                    context,
                    cfg) =>
                {
                    RabbitMqOptions rmq = context.GetRequiredService<IOptions<RabbitMqOptions>>()
                       .Value;

                    cfg.Host(
                        host: rmq.Host,
                        port: rmq.Port,
                        virtualHost: rmq.VirtualHost,
                        configure: h =>
                        {
                            h.Username(rmq.Username);
                            h.Password(rmq.Password);
                        });

                    cfg.ConfigureEndpoints(context);
                });
            });

            services.AddScoped<IPermissionsVersionStore, CachedPermissionsVersionStore>();

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
