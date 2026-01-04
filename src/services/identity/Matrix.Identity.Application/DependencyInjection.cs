using System.Reflection;
using FluentValidation;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Behaviors;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Application.Abstractions.Services.Validation;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Application.Services;
using Matrix.Identity.Application.Services.SecurityState;
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

            // Security state change collector
            services.AddScoped<ISecurityStateChangeCollector, SecurityStateChangeCollector>();

            // Validators
            services.AddScoped<IRoleIdsValidator, RoleIdsValidator>();
            services.AddScoped<IPermissionKeysValidator, PermissionKeysValidator>();

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
