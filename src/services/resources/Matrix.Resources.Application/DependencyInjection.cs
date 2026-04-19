using System.Reflection;
using FluentValidation;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.DependencyInjection;
using Matrix.Resources.Application.Errors;
using Matrix.Resources.Application.Scenarios.ClassicCity;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Resources.Application
{
    public static class DependencyInjection
    {
        public static void AddApplication(this IServiceCollection services)
        {
            Assembly assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(assembly); });
            services.AddValidatorsFromAssembly(assembly);
            services.AddScoped<IValidationExceptionFactory, ResourcesValidationErrorFactory>();
            services.AddClassicCityScenarioApplication();
            services.AddDefaultApplicationPipeline();
        }
    }
}
