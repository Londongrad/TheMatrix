using Matrix.BuildingBlocks.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.BuildingBlocks.Application.DependencyInjection
{
    public static class ApplicationPipelineServiceCollectionExtensions
    {
        public static IServiceCollection AddDefaultApplicationPipeline(this IServiceCollection services)
        {
            services.AddTransient(
                serviceType: typeof(IPipelineBehavior<,>),
                implementationType: typeof(LoggingBehavior<,>));
            services.AddTransient(
                serviceType: typeof(IPipelineBehavior<,>),
                implementationType: typeof(PermissionBehavior<,>));
            services.AddTransient(
                serviceType: typeof(IPipelineBehavior<,>),
                implementationType: typeof(ValidationBehavior<,>));

            return services;
        }
    }
}
