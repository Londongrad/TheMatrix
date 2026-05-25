using System.Reflection;
using FluentValidation;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.DependencyInjection;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Administration;
using Matrix.Identity.Application.Abstractions.Services.SecurityState;
using Matrix.Identity.Application.Abstractions.Services.Validation;
using Matrix.Identity.Application.Errors;
using Matrix.Identity.Application.Services;
using Matrix.Identity.Application.Services.Identity;
using Matrix.Identity.Application.Services.SecurityState;
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

            services.AddScoped<ISecurityStateChangeCollector, SecurityStateChangeCollector>();
            services.AddScoped<IAdminUserGuard, AdminUserGuard>();
            services.AddScoped<IOneTimeTokenDeliveryService, OneTimeTokenDeliveryService>();
            services.AddScoped<IPendingEmailChangeDeliveryService, PendingEmailChangeDeliveryService>();

            services.AddScoped<IRoleIdsValidator, RoleIdsValidator>();
            services.AddScoped<IPermissionKeysValidator, PermissionKeysValidator>();

            services.AddDefaultApplicationPipeline();
        }
    }
}
