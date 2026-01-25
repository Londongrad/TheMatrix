using System.Reflection;
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

            // Services
            services.AddScoped<ISimulationAdvanceExecutor, SimulationAdvanceExecutor>();

            return services;
        }
    }
}
