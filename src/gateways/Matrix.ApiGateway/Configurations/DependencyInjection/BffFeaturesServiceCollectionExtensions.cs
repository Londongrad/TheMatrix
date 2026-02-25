using MassTransit;
using Matrix.ApiGateway.Authorization.AuthContext;
using Matrix.ApiGateway.Authorization.AuthContext.Abstractions;
using Matrix.ApiGateway.Authorization.PermissionsVersion;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Abstractions;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Options;
using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.Consumers;
using Matrix.ApiGateway.DownstreamClients.Identity.Internal.PermissionsVersion;
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
            services.AddRabbitMqOptions(configuration);
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
                    validation: o => !string.IsNullOrWhiteSpace(o.ApiKey),
                    failureMessage: "IdentityInternal:ApiKey is required.")
               .Validate(
                    validation: o => o.RequestTimeoutSeconds > 0,
                    failureMessage: "IdentityInternal:RequestTimeoutSeconds must be greater than 0.")
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
                    failureMessage: "PermissionsVersion:StaleCacheTtlSeconds must be greater than or equal to CacheTtlSeconds.")
               .ValidateOnStart();

            return services;
        }

        private static IServiceCollection AddClassicCitySetupSessionOptions(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<ClassicCitySetupSessionOptions>()
               .Bind(configuration.GetSection(ClassicCitySetupSessionOptions.SectionName))
               .Validate(
                    validation: o => o.CacheTtlHours > 0,
                    failureMessage: "ClassicCitySetupSessions:CacheTtlHours must be greater than 0.")
               .Validate(
                    validation: o => o.MutationLockLeaseSeconds > 0,
                    failureMessage: "ClassicCitySetupSessions:MutationLockLeaseSeconds must be greater than 0.")
               .Validate(
                    validation: o => o.MutationLockAcquireTimeoutMilliseconds > 0,
                    failureMessage: "ClassicCitySetupSessions:MutationLockAcquireTimeoutMilliseconds must be greater than 0.")
               .Validate(
                    validation: o => o.MutationLockRetryDelayMilliseconds > 0,
                    failureMessage: "ClassicCitySetupSessions:MutationLockRetryDelayMilliseconds must be greater than 0.")
               .Validate(
                    validation: o => o.MutationLockAcquireTimeoutMilliseconds >= o.MutationLockRetryDelayMilliseconds,
                    failureMessage: "ClassicCitySetupSessions:MutationLockAcquireTimeoutMilliseconds must be greater than or equal to MutationLockRetryDelayMilliseconds.")
               .ValidateOnStart();

            return services;
        }

        private static IServiceCollection AddRabbitMqOptions(
            this IServiceCollection services,
            IConfiguration configuration)
        {
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
                    throw new InvalidOperationException("Redis:ConnectionString is required for gateway session orchestration.");

                ConfigurationOptions options = ConfigurationOptions.Parse(connectionString);
                options.AbortOnConnectFail = false;

                return ConnectionMultiplexer.Connect(options);
            });

            return services;
        }

        private static IServiceCollection AddIdentityInternalUsersClient(this IServiceCollection services)
        {
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

                client.DefaultRequestHeaders.Remove("X-Internal-Key");
                client.DefaultRequestHeaders.Add(
                    name: "X-Internal-Key",
                    value: options.ApiKey);
            });

            return services;
        }

        private static IServiceCollection AddGatewayMessaging(this IServiceCollection services)
        {
            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.AddConsumer<ClassicCitySetupLaunchRequestedConsumer>();
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

            return services;
        }
    }
}
