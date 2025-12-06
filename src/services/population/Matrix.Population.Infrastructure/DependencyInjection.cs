using Matrix.Population.Application.Abstractions;
using Matrix.Population.Infrastructure.Persistence;
using Matrix.Population.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Population.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("PopulationDb")
                                      ?? throw new InvalidOperationException(
                                          "Connection string 'PopulationDb' is not configured.");

            services.AddDbContext<PopulationDbContext>(options => { options.UseNpgsql(connectionString); });

            services.AddScoped<IPersonReadRepository, PersonReadRepository>();
            services.AddScoped<IPersonWriteRepository, PersonWriteRepository>();

            return services;
        }
    }
}
