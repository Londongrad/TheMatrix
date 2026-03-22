using System.Reflection;
using FluentValidation;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Behaviors;
using Matrix.SimulationSystems.Application.Errors;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.SimulationSystems.Application
{
    public static class DependencyInjection
    {
        public static void AddApplication(this IServiceCollection services)
        {
            Assembly assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(assembly); });
            services.AddValidatorsFromAssembly(assembly);
            services.AddScoped<IValidationExceptionFactory, SimulationSystemsValidationErrorFactory>();
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
        }
    }
}
