using Matrix.CityCore.Application.Abstractions;
using Matrix.CityCore.Infrastructure.Messaging;
using Matrix.CityCore.Infrastructure.Persistence;
using Matrix.CityCore.Infrastructure.Persistence.Repositories;
using Matrix.CityCore.Infrastructure.Simulation;
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

            services.AddScoped<ICityClockRepository, CityClockRepository>();
            services.AddScoped<ICityCoreUnitOfWork, CityCoreUnitOfWork>();
            services.AddScoped<ICityIntegrationEventPublisher, LoggingCityIntegrationEventPublisher>();

            // Настройки симуляции – пока хардкод, потом можно из конфигурации
            services.AddSingleton(
                new SimulationLoopSettings
                {
                    RealTimeTickMilliseconds = 1000,
                    SimMinutesPerTick = 5
                });

            services.AddHostedService<SimulationBackgroundService>();

            return services;
        }
    }
}
