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
               .ValidateOnStart();

            services.AddOptions<PermissionsVersionOptions>()
               .Bind(configuration.GetSection(PermissionsVersionOptions.SectionName))
               .Validate(
                    validation: o => o.CacheTtlSeconds > 0,
                    failureMessage: "PermissionsVersion:CacheTtlSeconds must be greater than 0.")
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
                client.Timeout = TimeSpan.FromSeconds(3);

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
