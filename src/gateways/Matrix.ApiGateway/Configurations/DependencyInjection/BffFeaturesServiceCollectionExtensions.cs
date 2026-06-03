using MassTransit;
using Matrix.ApiGateway.Authorization.AuthContext;
using Matrix.ApiGateway.Authorization.AuthContext.Abstractions;
using Matrix.ApiGateway.Authorization.PermissionsVersion;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Abstractions;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Options;
using Matrix.ApiGateway.Consumers;
using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Identity;
using Matrix.ApiGateway.DownstreamClients.Identity.Internal.PermissionsVersion;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;
using Matrix.BuildingBlocks.Api.OptionsValidation;
using Matrix.BuildingBlocks.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Matrix.ApiGateway.Configurations.DependencyInjection
{
    public static class BffFeaturesServiceCollectionExtensions
    {
        public static IServiceCollection AddBffFeatures(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddPermissionsVersionOptions(configuration);
            services.AddDownstreamReadResilience(configuration);
            services.AddRabbitMqOptions(configuration);
            services.AddMassTransitEndpointHygieneOptions(configuration);
            services.AddClassicCitySetupSessionOptions(configuration);
            services.AddGatewayRedisCache(configuration);
            services.AddIdentityInternalUsersClient();
            services.AddGatewayMessaging();

            services.AddScoped<IPermissionsVersionStore, CachedPermissionsVersionStore>();
            services.AddScoped<IAuthContextStore, CachedAuthContextStore>();

            return services;
        }

        private static IServiceCollection AddPermissionsVersionOptions(
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
                    validation: o => o.RequestTimeoutSeconds > 0,
                    failureMessage: "IdentityInternal:RequestTimeoutSeconds must be greater than 0.")
               .ValidateInternalApiKeyRing(IdentityInternalOptions.SectionName)
               .ValidateOnStart();

            services.AddOptions<PermissionsVersionOptions>()
               .Bind(configuration.GetSection(PermissionsVersionOptions.SectionName))
               .Validate(
                    validation: o => o.CacheTtlSeconds > 0,
                    failureMessage: "PermissionsVersion:CacheTtlSeconds must be greater than 0.")
               .Validate(
                    validation: o => o.StaleCacheTtlSeconds > 0,
                    failureMessage: "PermissionsVersion:StaleCacheTtlSeconds must be greater than 0.")
               .Validate(
                    validation: o => o.StaleCacheTtlSeconds >= o.CacheTtlSeconds,
                    failureMessage:
                    "PermissionsVersion:StaleCacheTtlSeconds must be greater than or equal to CacheTtlSeconds.")
               .ValidateOnStart();

            return services;
        }

        private static IServiceCollection AddGatewayRedisCache(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddStackExchangeRedisCache(o =>
            {
                o.Configuration = configuration["Redis:ConnectionString"];
                o.InstanceName = configuration["Redis:InstanceName"];
            });

            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                string? connectionString = configuration["Redis:ConnectionString"];

                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException(
                        "Redis:ConnectionString is required for gateway session orchestration.");

                var options = ConfigurationOptions.Parse(connectionString);
                options.AbortOnConnectFail = false;

                return ConnectionMultiplexer.Connect(options);
            });

            return services;
        }

        private static IServiceCollection AddIdentityInternalUsersClient(this IServiceCollection services)
        {
            services.AddTransient<InternalIdentityApiKeyAuthenticationHandler>();

            services.AddHttpClient<IIdentityInternalUsersClient, IdentityInternalUsersClient>((
                    sp,
                    client) =>
                {
                    IdentityInternalOptions options = sp.GetRequiredService<IOptions<IdentityInternalOptions>>()
                       .Value;

                    client.BaseAddress = new Uri(
                        uriString: options.BaseUrl,
                        uriKind: UriKind.Absolute);
                    client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
                })
               .AddDownstreamReadResilience(serviceName: DownstreamServiceNames.Identity)
               .AddHttpMessageHandler<InternalIdentityApiKeyAuthenticationHandler>();

            return services;
        }

        private static IServiceCollection AddGatewayMessaging(this IServiceCollection services)
        {
            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.AddRabbitMqEndpointHygiene();
                x.AddClassicCitySetupSessionConsumers();
                x.AddConsumer<UserSecurityStateChangedConsumer>();
                x.AddConsumer<DefaultUserAccessPolicyChangedConsumer>();

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

            return services;
        }
    }
}
