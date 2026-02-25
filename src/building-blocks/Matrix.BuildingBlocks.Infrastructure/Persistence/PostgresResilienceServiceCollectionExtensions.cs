using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.BuildingBlocks.Infrastructure.Persistence
{
    public static class PostgresResilienceServiceCollectionExtensions
    {
        public static IServiceCollection AddPostgresResilienceOptions(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<PostgresResilienceOptions>()
               .Bind(configuration.GetSection(PostgresResilienceOptions.SectionName))
               .Validate(
                    validation: o => o.MaxRetryCount > 0,
                    failureMessage: "PostgresResilience:MaxRetryCount must be greater than 0.")
               .Validate(
                    validation: o => o.MaxRetryDelaySeconds > 0,
                    failureMessage: "PostgresResilience:MaxRetryDelaySeconds must be greater than 0.")
               .ValidateOnStart();

            return services;
        }
    }
}
