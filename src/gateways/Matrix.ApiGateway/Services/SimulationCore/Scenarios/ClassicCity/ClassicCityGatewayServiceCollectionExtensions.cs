using MassTransit;
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

        public static IServiceCollection AddClassicCityGateway(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddClassicCityDashboard(configuration);
            services.AddClassicCityCities();
            services.AddClassicCitySetupSessionOptions(configuration);
            services.AddClassicCitySetupSessionServices();

            return services;
        }
    }
}
