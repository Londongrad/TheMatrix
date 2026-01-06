using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.BuildingBlocks.Api.Authorization
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMatrixPermissionsPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                foreach (string key in PermissionsCatalog.AllKeys)
                    options.AddPolicy(
                        name: key,
                        configurePolicy: policy =>
                        {
                            policy.RequireAuthenticatedUser();
                            policy.RequireClaim(
                                claimType: PermissionClaimTypes.Permission,
                                key);
                        });
            });

            return services;
        }
    }
}
