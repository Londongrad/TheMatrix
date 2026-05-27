using System.Reflection;
using FluentValidation;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.DependencyInjection;
using Matrix.SimulationCore.Application.Errors;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity;
using Matrix.SimulationCore.Application.Services.Simulation;
using Matrix.SimulationCore.Application.Services.Simulation.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Matrix.SimulationCore.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            Assembly assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(assembly); });
            services.AddValidatorsFromAssembly(assembly);

            services.AddScoped<ISimulationAdvanceExecutor, SimulationAdvanceExecutor>();
            services.AddScoped<SimulationScenarioAdvanceHandlerRegistry>();
            services.TryAddSingleton<ISimulationFixedStepSettings, DefaultSimulationFixedStepSettings>();
            services.AddScoped<IValidationExceptionFactory, SimulationCoreValidationErrorFactory>();
            services.AddClassicCityScenarioApplication();
            services.AddDefaultApplicationPipeline();

            return services;
        }
    }
}
