using Matrix.SimulationCore.Application.Abstractions.Outbox;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence;
using Matrix.SimulationCore.Infrastructure.Outbox;
using Matrix.SimulationCore.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddClassicCityScenarioInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<ICityRepository, CityRepository>();
            services.AddScoped<IDistrictRepository, DistrictRepository>();
            services.AddScoped<IResidentialBuildingRepository, ResidentialBuildingRepository>();
            services.AddScoped<ICityAnchorRepository, CityAnchorRepository>();
            services.AddScoped<IRoadNodeRepository, RoadNodeRepository>();
            services.AddScoped<IRoadSegmentRepository, RoadSegmentRepository>();
            services.AddScoped<ICityActiveTripRepository, CityActiveTripRepository>();
            services.AddScoped<ICityWeatherRepository, CityWeatherRepository>();
            services.AddScoped<ISimulationCoreOutboxWriter, SimulationCoreOutboxWriter>();

            return services;
        }
    }
}
