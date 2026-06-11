using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddClassicCityScenarioApplication(this IServiceCollection services)
        {
            services.AddSingleton<CityEnvironmentalConditionPolicy>();
            services.AddSingleton<CityMaintenanceBudgetGuard>();
            services.AddSingleton<ClassicCityWeatherPressureProfileFactory>();
            services.AddSingleton<ClassicCityRoadSegmentConditionProjectionPolicy>();
            services.AddSingleton<ClassicCityDistrictHeatingProjectionPolicy>();
            services.AddSingleton<ClassicCityDistrictWaterDistributionProjectionPolicy>();
            services.AddSingleton<ClassicCityDistrictPowerDistributionProjectionPolicy>();
            services.AddSingleton<ClassicCityDistrictSanitationProjectionPolicy>();
            services.AddSingleton<ClassicCityDistrictUtilityIncidentProjectionPolicy>();
            services.AddScoped<CityMaintenanceBudgetAuthorizationService>();

            return services;
        }
    }
}
