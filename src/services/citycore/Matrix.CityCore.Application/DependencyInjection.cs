using System.Reflection;
using Matrix.CityCore.Application.Scenarios.ClassicCity;
using Matrix.CityCore.Application.Scenarios.ClassicCity.Services.Bootstrap;
using Matrix.CityCore.Application.Services.Simulation;
using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.CityCore.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            Assembly assembly = typeof(DependencyInjection).Assembly;
            services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(assembly); });

            services.AddScoped<ISimulationAdvanceExecutor, SimulationAdvanceExecutor>();
            services.AddClassicCityScenarioApplication();

            return services;
        }
    }
}
