using MassTransit;
using Matrix.ApiGateway.Configurations.DependencyInjection;
using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Economy.Scenarios.ClassicCity;
using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.ApiGateway.DownstreamClients.Population.Scenarios.ClassicCity;
using Matrix.ApiGateway.DownstreamClients.Resources.Scenarios.ClassicCity.Stockpiles;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.DownstreamClients.SimulationCore.Scenarios.ClassicCity.Trips;
using Matrix.ApiGateway.DownstreamClients.SimulationSystems.Scenarios.ClassicCity.EnvironmentalConditions;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.Dashboard;
using Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity.SetupSessions;

namespace Matrix.ApiGateway.Services.SimulationCore.Scenarios.ClassicCity
{
    public static class ClassicCityGatewayServiceCollectionExtensions
    {
        public static void AddClassicCityGatewayConsumers(this IBusRegistrationConfigurator configurator)
        {
            configurator.AddClassicCitySetupSessionConsumers();
        }

        private static IServiceCollection AddClassicCityDownstreamClients(this IServiceCollection services)
        {
            services.AddInternalDownstreamClient<IClassicCityEconomyApiClient, ClassicCityEconomyApiClient>(
                DownstreamServiceNames.Economy);
            services.AddInternalDownstreamClient<IClassicCityPopulationApiClient, PopulationApiClient>(
                DownstreamServiceNames.Population);
            services.AddInternalDownstreamClient<ICitiesApiClient, CitiesApiClient>(
                DownstreamServiceNames.SimulationCore);
            services.AddInternalDownstreamClient<ITripsApiClient, TripsApiClient>(
                DownstreamServiceNames.SimulationCore);
            services.AddInternalDownstreamClient<IStockpilesApiClient, StockpilesApiClient>(
                DownstreamServiceNames.Resources);
            services.AddInternalDownstreamClient<IEnvironmentalConditionsApiClient, EnvironmentalConditionsApiClient>(
                DownstreamServiceNames.SimulationSystems);

            return services;
        }

        public static IServiceCollection AddClassicCityGateway(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddClassicCityDashboard(configuration);
            services.AddClassicCityCities();
            services.AddClassicCitySetupSessionOptions(configuration);
            services.AddClassicCitySetupSessionServices();
            services.AddClassicCityDownstreamClients();

            return services;
        }
    }
}
