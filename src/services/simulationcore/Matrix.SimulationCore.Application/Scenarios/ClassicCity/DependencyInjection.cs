using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Bootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Simulation;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Topology;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Topology.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Weather;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Weather.Abstractions;
using Matrix.SimulationCore.Application.Services.Bootstrap.Abstractions;
using Matrix.SimulationCore.Application.Services.Generation;
using Matrix.SimulationCore.Application.Services.Generation.Abstractions;
using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddClassicCityScenarioApplication(this IServiceCollection services)
        {
            services.AddScoped<IWeatherAdvanceExecutor, WeatherAdvanceExecutor>();
            services.AddSingleton<ICityGenerationContentCatalog, CityGenerationContentCatalog>();
            services.AddSingleton<ICityNameSuggestionService, CityNameSuggestionService>();
            services.AddSingleton<ICitySimulationBootstrapStrategy, ClassicCitySimulationBootstrapStrategy>();
            services.AddSingleton<ICityTopologyBootstrapFactory, CityTopologyBootstrapFactory>();
            services.AddSingleton<IWeatherStatePlanner, WeatherStatePlanner>();
            services.AddSingleton<ICityWeatherBootstrapFactory, CityWeatherBootstrapFactory>();
            services.AddScoped<ISimulationScenarioAdvanceHandler, ClassicCitySimulationAdvanceHandler>();

            return services;
        }
    }
}
