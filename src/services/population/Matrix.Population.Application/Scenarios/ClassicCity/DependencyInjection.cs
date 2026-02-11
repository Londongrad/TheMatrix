using Matrix.Population.Application.Scenarios.ClassicCity.Services.Generation;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Population.Application.Scenarios.ClassicCity
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddClassicCityScenarioApplication(this IServiceCollection services)
        {
            services.AddSingleton<IPopulationGenerationContentCatalog, PopulationGenerationContentCatalog>();
            services.AddSingleton<CityPopulationBootstrapGenerator>();
            services.AddSingleton<CityPopulationClimateAdaptationPolicy>();
            services.AddSingleton<CityPopulationWeatherImpactPolicy>();
            services.AddSingleton<CityPopulationWeatherExposurePolicy>();

            return services;
        }
    }
}
