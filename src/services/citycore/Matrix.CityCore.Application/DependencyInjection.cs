using System.Reflection;
using FluentValidation;
using Matrix.CityCore.Application.Scenarios.ClassicCity;
using Matrix.CityCore.Application.Scenarios.ClassicCity.Services.Bootstrap;
using Matrix.CityCore.Application.Services.Simulation;
using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using Matrix.CityCore.Application.Errors;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.CityCore.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            Assembly assembly = typeof(DependencyInjection).Assembly;
            services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(assembly); });
            services.AddValidatorsFromAssembly(assembly);

            services.AddScoped<ISimulationAdvanceExecutor, SimulationAdvanceExecutor>();
            services.AddScoped<IValidationExceptionFactory, CityCoreValidationErrorFactory>();
            services.AddClassicCityScenarioApplication();

            services.AddTransient(
                serviceType: typeof(IPipelineBehavior<,>),
                implementationType: typeof(LoggingBehavior<,>));
            services.AddTransient(
                serviceType: typeof(IPipelineBehavior<,>),
                implementationType: typeof(ValidationBehavior<,>));
            services.AddTransient(
                serviceType: typeof(IPipelineBehavior<,>),
                implementationType: typeof(PermissionBehavior<,>));

            return services;
        }
    }
}
