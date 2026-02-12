using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Outbox;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Infrastructure.Outbox;
using Matrix.CityCore.Infrastructure.Persistence.Repositories;
using Matrix.CityCore.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.CityCore.Infrastructure.Scenarios.ClassicCity
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddClassicCityScenarioInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<ISimulationHostReadRepository, ClassicCitySimulationHostReadRepository>();
            services.AddScoped<ICityRepository, CityRepository>();
            services.AddScoped<IDistrictRepository, DistrictRepository>();
            services.AddScoped<IResidentialBuildingRepository, ResidentialBuildingRepository>();
            services.AddScoped<ICityWeatherRepository, CityWeatherRepository>();
            services.AddScoped<ICityCoreOutboxWriter, CityCoreOutboxWriter>();

            return services;
        }
    }
}
