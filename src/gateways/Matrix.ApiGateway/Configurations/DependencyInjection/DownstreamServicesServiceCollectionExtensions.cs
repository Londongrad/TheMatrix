using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.DownstreamClients.CityCore.Cities;
using Matrix.ApiGateway.DownstreamClients.CityCore.Simulation;
using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Economy;
using Matrix.ApiGateway.DownstreamClients.HttpHandlers;
using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Permissions;
using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Roles;
using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Users;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Account;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Assets;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Auth;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Sessions;
using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.ApiGateway.DownstreamClients.Population.Person;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.Configurations.DependencyInjection
{
    public static class DownstreamServicesServiceCollectionExtensions
    {
        public static IServiceCollection AddDownstreamServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<DownstreamServicesOptions>()
               .Bind(configuration.GetSection(DownstreamServicesOptions.SectionName))
               .Validate(
                    validation: o => IsAbsoluteUri(o.CityCore),
                    failureMessage: $"{DownstreamServicesOptions.SectionName}:CityCore must be an absolute URI.")
               .Validate(
                    validation: o => IsAbsoluteUri(o.Economy),
                    failureMessage: $"{DownstreamServicesOptions.SectionName}:Economy must be an absolute URI.")
               .Validate(
                    validation: o => IsAbsoluteUri(o.Population),
                    failureMessage: $"{DownstreamServicesOptions.SectionName}:Population must be an absolute URI.")
               .Validate(
                    validation: o => IsAbsoluteUri(o.Identity),
                    failureMessage: $"{DownstreamServicesOptions.SectionName}:Identity must be an absolute URI.")
               .ValidateOnStart();

            services.AddHttpContextAccessor();
            services.AddTransient<ForwardClientInfoHeadersHandler>();
            services.AddTransient<ForwardAuthorizationHeaderHandler>();
            services.AddTransient<InternalJwtExchangeHandler>();

            services.AddCityCoreClients();
            services.AddEconomyClients();
            services.AddPopulationClients();
            services.AddIdentityClients();

            return services;
        }

        private static IServiceCollection AddCityCoreClients(this IServiceCollection services)
        {
            services.AddHttpClient<ISimulationApiClient, SimulationApiClient>((
                        sp,
                        client) =>
                    ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: DownstreamServiceNames.CityCore))
               .AddHttpMessageHandler<InternalJwtExchangeHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            services.AddHttpClient<ICitiesApiClient, CitiesApiClient>((
                        sp,
                        client) =>
                    ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: DownstreamServiceNames.CityCore))
               .AddHttpMessageHandler<InternalJwtExchangeHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            return services;
        }

        private static IServiceCollection AddEconomyClients(this IServiceCollection services)
        {
            services.AddHttpClient<IEconomyApiClient, EconomyApiClient>((
                        sp,
                        client) =>
                    ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: DownstreamServiceNames.Economy))
               .AddHttpMessageHandler<InternalJwtExchangeHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            return services;
        }

        private static IServiceCollection AddPopulationClients(this IServiceCollection services)
        {
            services.AddHttpClient<IPersonApiClient, PersonApiClient>((
                        sp,
                        client) =>
                    ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: DownstreamServiceNames.Population))
               .AddHttpMessageHandler<InternalJwtExchangeHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            services.AddHttpClient<IPopulationApiClient, PopulationApiClient>((
                        sp,
                        client) =>
                    ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: DownstreamServiceNames.Population))
               .AddHttpMessageHandler<InternalJwtExchangeHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            return services;
        }

        private static IServiceCollection AddIdentityClients(this IServiceCollection services)
        {
            services.AddHttpClient<IIdentityAuthClient, IdentityAuthApiClient>((
                        sp,
                        client) =>
                    ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: DownstreamServiceNames.Identity))
               .AddHttpMessageHandler<ForwardClientInfoHeadersHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            services.AddHttpClient<IIdentityAccountClient, IdentityAccountApiClient>((
                        sp,
                        client) =>
                    ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: DownstreamServiceNames.Identity))
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            services.AddHttpClient<IIdentityAssetsClient, IdentityAssetsApiClient>((
                        sp,
                        client) =>
                    ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: DownstreamServiceNames.Identity))
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            services.AddHttpClient<IIdentitySessionsClient, IdentitySessionsApiClient>((
                        sp,
                        client) =>
                    ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: DownstreamServiceNames.Identity))
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .AddHttpMessageHandler<ForwardClientInfoHeadersHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            services.AddHttpClient<IIdentityAdminRolesClient, IdentityAdminRolesApiClient>((
                        sp,
                        client) =>
                    ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: DownstreamServiceNames.Identity))
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            services.AddHttpClient<IIdentityAdminPermissionsClient, IdentityAdminPermissionsApiClient>((
                        sp,
                        client) =>
                    ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: DownstreamServiceNames.Identity))
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            services.AddHttpClient<IIdentityAdminUsersClient, IdentityAdminUsersApiClient>((
                        sp,
                        client) =>
                    ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: DownstreamServiceNames.Identity))
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .ConfigureHttpClient(ConfigureTimeout);

            return services;
        }

        private static void ConfigureServiceBaseAddress(
            IServiceProvider sp,
            HttpClient client,
            string serviceName)
        {
            DownstreamServicesOptions options = sp.GetRequiredService<IOptions<DownstreamServicesOptions>>()
               .Value;

            string baseAddress = serviceName switch
            {
                DownstreamServiceNames.CityCore => options.CityCore,
                DownstreamServiceNames.Economy => options.Economy,
                DownstreamServiceNames.Population => options.Population,
                DownstreamServiceNames.Identity => options.Identity,
                _ => throw new InvalidOperationException($"Unsupported downstream service '{serviceName}'.")
            };

            client.BaseAddress = new Uri(
                uriString: baseAddress,
                uriKind: UriKind.Absolute);
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

        private static bool IsAbsoluteUri(string value)
        {
            return Uri.TryCreate(
                uriString: value,
                uriKind: UriKind.Absolute,
                result: out _);
        }
    }
}
