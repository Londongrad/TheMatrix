using System.Reflection;
using FluentValidation;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.DependencyInjection;
using Matrix.Population.Application.Errors;
using Matrix.Population.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Population.Application
{
    public static class DependencyInjection
    {
        public static void AddApplication(this IServiceCollection services)
        {
            Assembly assembly = typeof(DependencyInjection).Assembly;

            services.AddSingleton<PersonNeedsProgressionPolicy>();
            services.AddSingleton<MarriageDomainService>();
            services.AddSingleton<PopulationBirthDomainService>();
            services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(assembly); });
            services.AddValidatorsFromAssembly(assembly);

            services.AddScoped<IValidationExceptionFactory, PopulationValidationErrorFactory>();
            services.AddDefaultApplicationPipeline();
        }
    }
}
