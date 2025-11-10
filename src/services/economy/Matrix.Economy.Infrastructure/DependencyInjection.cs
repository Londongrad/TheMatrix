using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Economy.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Economy.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("EconomyDb")
                ?? throw new InvalidOperationException("Connection string 'EconomyDb' is not configured.");

            services.AddDbContext<EconomyDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });

            services.AddScoped<ICityBudgetRepository, CityBudgetRepository>();
            services.AddScoped<IEconomyUnitOfWork, EconomyUnitOfWork>();

            return services;
        }
    }
}
