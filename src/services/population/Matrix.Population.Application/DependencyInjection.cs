using System.Reflection;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Behaviors;
using Matrix.Population.Application.Errors;
using Matrix.Population.Application.Services.Generation;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.Services.Abstractions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Population.Application
{
    public static class DependencyInjection
    {
        public static void AddApplication(this IServiceCollection services)
        {
            Assembly assembly = typeof(DependencyInjection).Assembly;

            services.AddSingleton<IPopulationGenerationContentCatalog, PopulationGenerationContentCatalog>();
            services.AddSingleton<CityPopulationBootstrapGenerator>();
            services.AddSingleton<CityPopulationClimateAdaptationPolicy>();
            services.AddSingleton<CityPopulationWeatherImpactPolicy>();
            services.AddSingleton<CityPopulationWeatherExposurePolicy>();

            services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(assembly); });

            services.AddScoped<IValidationExceptionFactory, PopulationValidationErrorFactory>();

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
