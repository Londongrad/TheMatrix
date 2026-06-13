using System.Reflection;
using FluentValidation;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.DependencyInjection;
using Matrix.Economy.Application.Errors;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Economy.Application
{
    public static class DependencyInjection
    {
        public static void AddApplication(this IServiceCollection services)
        {
            Assembly assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(assembly); });
            services.AddValidatorsFromAssembly(assembly);
            services.AddScoped<IValidationExceptionFactory, EconomyValidationErrorFactory>();

            services.AddDefaultApplicationPipeline();
        }
    }
}
