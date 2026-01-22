using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.Outbox.DependencyInjection;
using Matrix.CityCore.Application.Abstractions.Outbox;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Infrastructure.HostedServices;
using Matrix.CityCore.Infrastructure.Options;
using Matrix.CityCore.Infrastructure.Outbox;
using Matrix.CityCore.Infrastructure.Persistence;
using Matrix.CityCore.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.CityCore.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("CityCoreDb") ??
                                      throw new InvalidOperationException(
                                          "Connection string 'CityCoreDb' is not configured.");

            services.AddDbContext<CityCoreDbContext>(options => { options.UseNpgsql(connectionString); });

            services.AddOptions<SimulationTickOptions>()
               .Bind(configuration.GetSection(SimulationTickOptions.SectionName));

            services.AddScoped<ISimulationClockRepository, SimulationClockRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ICityCoreOutboxWriter, CityCoreOutboxWriter>();

            services.AddOutbox<CityCoreDbContext>(configuration);
            services.AddScoped<IOutboxMessagePublisher, LoggingOutboxMessagePublisher>();

            services.AddHostedService<SimulationTickHostedService>();

            return services;
        }
    }
}
