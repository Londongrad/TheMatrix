using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.BuildingBlocks.Infrastructure.Authorization.Claims
{
    public static class ClaimsExtensions
    {
        public static IServiceCollection AddPermissionCheckingFromClaims(this IServiceCollection services)
        {
            // Чтобы ClaimsPermissionChecker мог читать HttpContext.User
            services.AddHttpContextAccessor();

            // Реализация со scoped-кэшем на запрос
            services.AddScoped<ClaimsPermissionChecker>();

            // Основной контракт, который использует PermissionBehavior
            services.AddScoped<IPermissionChecker>(sp => sp.GetRequiredService<ClaimsPermissionChecker>());

            return services;
        }
    }
}
