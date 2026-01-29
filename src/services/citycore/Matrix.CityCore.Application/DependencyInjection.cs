using System.Reflection;
using Matrix.CityCore.Application.Services.Generation;
using Matrix.CityCore.Application.Services.Generation.Abstractions;
using Matrix.CityCore.Application.Services.Simulation;
using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using Matrix.CityCore.Application.Services.Topology;
using Matrix.CityCore.Application.Services.Topology.Abstractions;
using Matrix.CityCore.Application.Services.Weather;
using Matrix.CityCore.Application.Services.Weather.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.CityCore.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            Assembly assembly = typeof(DependencyInjection).Assembly;
            services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(assembly); });

            // Services
            services.AddScoped<ISimulationAdvanceExecutor, SimulationAdvanceExecutor>();
            services.AddScoped<IWeatherAdvanceExecutor, WeatherAdvanceExecutor>();
            services.AddSingleton<ICityGenerationContentCatalog, CityGenerationContentCatalog>();
            services.AddSingleton<ICityNameSuggestionService, CityNameSuggestionService>();
            services.AddSingleton<ICityTopologyBootstrapFactory, CityTopologyBootstrapFactory>();
            services.AddSingleton<IWeatherStatePlanner, WeatherStatePlanner>();
            services.AddSingleton<ICityWeatherBootstrapFactory, CityWeatherBootstrapFactory>();

            return services;
        }
    }
}