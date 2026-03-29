using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity
{
    internal static class DependencyInjection
    {
        public static IServiceCollection AddClassicCityScenarioApplication(this IServiceCollection services)
        {
            services.AddSingleton<CityEnvironmentalConditionPolicy>();
            services.AddSingleton<CityMaintenanceBudgetGuard>();
            services.AddSingleton<ClassicCityWeatherPressureProfileFactory>();

            return services;
        }
    }
}
