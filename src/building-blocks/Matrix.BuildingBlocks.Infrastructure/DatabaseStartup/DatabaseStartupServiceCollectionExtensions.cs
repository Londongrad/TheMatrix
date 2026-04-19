using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.BuildingBlocks.Infrastructure.DatabaseStartup
{
    public static class DatabaseStartupServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabaseStartup(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<DatabaseStartupOptions>()
               .BindConfiguration(DatabaseStartupOptions.SectionName);

            return services;
        }
    }
}
