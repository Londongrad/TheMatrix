using System.Reflection;
using FluentValidation;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Behaviors;
using Matrix.Identity.Application.Errors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Identity.Application
{
    public static class DependencyInjection
    {
        public static void AddApplication(this IServiceCollection services)
        {
            Assembly assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(assembly); });

            services.AddValidatorsFromAssembly(assembly);

            services.AddScoped<IValidationExceptionFactory, IdentityValidationErrorFactory>();

            // Behaviors (используем общие из BuildingBlocks)ors
            services.AddTransient(
                serviceType: typeof(IPipelineBehavior<,>),
                implementationType: typeof(LoggingBehavior<,>));
            services.AddTransient(
                serviceType: typeof(IPipelineBehavior<,>),
                implementationType: typeof(ValidationBehavior<,>));
        }
    }
}
