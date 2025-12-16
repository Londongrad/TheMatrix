using Matrix.BuildingBlocks.Application.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.BuildingBlocks.Api.Authorization
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMatrixPermissionsPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                foreach (PermissionDefinition p in PermissionsCatalog.All)
                    options.AddPolicy(
                        name: p.Key,
                        configurePolicy: policy =>
                            policy.RequireClaim(
                                claimType: PermissionClaimTypes.Permission,
                                p.Key));
            });

            return services;
        }
    }
}
