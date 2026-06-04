using Matrix.ApiGateway.Configurations.Options;
using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Economy;
using Matrix.ApiGateway.DownstreamClients.HttpHandlers;
using Matrix.ApiGateway.DownstreamClients.Identity;
using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Permissions;
using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Roles;
using Matrix.ApiGateway.DownstreamClients.Identity.Admin.Users;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Account;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Assets;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Auth;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Sessions;
using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.ApiGateway.DownstreamClients.Population.Person;
using Matrix.ApiGateway.DownstreamClients.Resources.Scenarios.ClassicCity.Stockpiles;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Trips;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Simulation;
using Matrix.ApiGateway.DownstreamClients.SimulationSystems.Scenarios.ClassicCity.EnvironmentalConditions;
using static Matrix.ApiGateway.Configurations.DependencyInjection.DownstreamHttpClientDefaults;

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
                    validation: o => IsAbsoluteUri(o.SimulationCore),
                    failureMessage: $"{DownstreamServicesOptions.SectionName}:SimulationCore must be an absolute URI.")
               .Validate(
                    validation: o => IsAbsoluteUri(o.SimulationSystems),
                    failureMessage:
                    $"{DownstreamServicesOptions.SectionName}:SimulationSystems must be an absolute URI.")
               .Validate(
                    validation: o => IsAbsoluteUri(o.Economy),
                    failureMessage: $"{DownstreamServicesOptions.SectionName}:Economy must be an absolute URI.")
               .Validate(
                    validation: o => IsAbsoluteUri(o.Resources),
                    failureMessage: $"{DownstreamServicesOptions.SectionName}:Resources must be an absolute URI.")
               .Validate(
                    validation: o => IsAbsoluteUri(o.Population),
                    failureMessage: $"{DownstreamServicesOptions.SectionName}:Population must be an absolute URI.")
               .Validate(
                    validation: o => IsAbsoluteUri(o.Identity),
                    failureMessage: $"{DownstreamServicesOptions.SectionName}:Identity must be an absolute URI.")
               .ValidateOnStart();
            services.AddDownstreamReadResilience(configuration);

            services.AddHttpContextAccessor();
            services.AddTransient<InternalIdentityApiKeyAuthenticationHandler>();
            services.AddTransient<TrustedIdentityClientContextHandler>();
            services.AddTransient<ForwardAuthorizationHeaderHandler>();
            services.AddTransient<InternalJwtExchangeHandler>();

            services.AddSimulationCoreClients();
            services.AddSimulationSystemsClients();
            services.AddEconomyClients();
            services.AddResourcesClients();
            services.AddPopulationClients();
            services.AddIdentityClients();

            return services;
        }

        private static IServiceCollection AddSimulationCoreClients(this IServiceCollection services)
        {
            services.AddInternalDownstreamClient<ISimulationApiClient, SimulationApiClient>(
                DownstreamServiceNames.SimulationCore);

            services.AddHttpClient<ICitiesApiClient, CitiesApiClient>((
                        sp,
                        client) =>
                    ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: DownstreamServiceNames.SimulationCore))
               .AddHttpMessageHandler<InternalJwtExchangeHandler>()
               .AddDownstreamReadResilience(serviceName: DownstreamServiceNames.SimulationCore)
               .ConfigureHttpClient(ConfigureTimeout);

            services.AddInternalDownstreamClient<ITripsApiClient, TripsApiClient>(
                DownstreamServiceNames.SimulationCore);

            return services;
        }

        private static IServiceCollection AddEconomyClients(this IServiceCollection services)
        {
            services.AddInternalDownstreamClient<IEconomyApiClient, EconomyApiClient>(
                DownstreamServiceNames.Economy);

            return services;
        }

        private static IServiceCollection AddResourcesClients(this IServiceCollection services)
        {
            services.AddInternalDownstreamClient<IStockpilesApiClient, StockpilesApiClient>(
                DownstreamServiceNames.Resources);

            return services;
        }

        private static IServiceCollection AddSimulationSystemsClients(this IServiceCollection services)
        {
            services.AddInternalDownstreamClient<IEnvironmentalConditionsApiClient, EnvironmentalConditionsApiClient>(
                DownstreamServiceNames.SimulationSystems);

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
               .AddDownstreamReadResilience(serviceName: DownstreamServiceNames.Population)
               .ConfigureHttpClient(ConfigureTimeout);

            services.AddHttpClient<IPopulationApiClient, PopulationApiClient>((
                        sp,
                        client) =>
                    ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: DownstreamServiceNames.Population))
               .AddHttpMessageHandler<InternalJwtExchangeHandler>()
               .AddDownstreamReadResilience(serviceName: DownstreamServiceNames.Population)
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
               .AddHttpMessageHandler<InternalIdentityApiKeyAuthenticationHandler>()
               .AddHttpMessageHandler<TrustedIdentityClientContextHandler>()
               .AddDownstreamReadResilience(serviceName: DownstreamServiceNames.Identity)
               .ConfigureHttpClient(ConfigureTimeout);

            services.AddHttpClient<IIdentityAccountClient, IdentityAccountApiClient>((
                        sp,
                        client) =>
                    ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: DownstreamServiceNames.Identity))
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .AddHttpMessageHandler<InternalIdentityApiKeyAuthenticationHandler>()
               .AddHttpMessageHandler<TrustedIdentityClientContextHandler>()
               .AddDownstreamReadResilience(serviceName: DownstreamServiceNames.Identity)
               .ConfigureHttpClient(ConfigureTimeout);

            services.AddHttpClient<IIdentityAssetsClient, IdentityAssetsApiClient>((
                        sp,
                        client) =>
                    ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: DownstreamServiceNames.Identity))
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .AddDownstreamReadResilience(serviceName: DownstreamServiceNames.Identity)
               .ConfigureHttpClient(ConfigureTimeout);

            services.AddHttpClient<IIdentitySessionsClient, IdentitySessionsApiClient>((
                        sp,
                        client) =>
                    ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: DownstreamServiceNames.Identity))
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .AddHttpMessageHandler<InternalIdentityApiKeyAuthenticationHandler>()
               .AddHttpMessageHandler<TrustedIdentityClientContextHandler>()
               .AddDownstreamReadResilience(serviceName: DownstreamServiceNames.Identity)
               .ConfigureHttpClient(ConfigureTimeout);

            services.AddHttpClient<IIdentityAdminRolesClient, IdentityAdminRolesApiClient>((
                        sp,
                        client) =>
                    ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: DownstreamServiceNames.Identity))
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .AddDownstreamReadResilience(serviceName: DownstreamServiceNames.Identity)
               .ConfigureHttpClient(ConfigureTimeout);

            services.AddHttpClient<IIdentityAdminPermissionsClient, IdentityAdminPermissionsApiClient>((
                        sp,
                        client) =>
                    ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: DownstreamServiceNames.Identity))
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .AddDownstreamReadResilience(serviceName: DownstreamServiceNames.Identity)
               .ConfigureHttpClient(ConfigureTimeout);

            services.AddHttpClient<IIdentityAdminUsersClient, IdentityAdminUsersApiClient>((
                        sp,
                        client) =>
                    ConfigureServiceBaseAddress(
                        sp: sp,
                        client: client,
                        serviceName: DownstreamServiceNames.Identity))
               .AddHttpMessageHandler<ForwardAuthorizationHeaderHandler>()
               .AddDownstreamReadResilience(serviceName: DownstreamServiceNames.Identity)
               .ConfigureHttpClient(ConfigureTimeout);

            return services;
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
