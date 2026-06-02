using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationCore.Infrastructure.Persistence
{
    public sealed partial class SimulationCoreDbContext
    {
        public DbSet<City> Cities => Set<City>();
        public DbSet<District> Districts => Set<District>();
        public DbSet<ResidentialBuilding> ResidentialBuildings => Set<ResidentialBuilding>();
        public DbSet<CityAnchor> CityAnchors => Set<CityAnchor>();
        public DbSet<RoadNode> RoadNodes => Set<RoadNode>();
        public DbSet<RoadSegment> RoadSegments => Set<RoadSegment>();
        public DbSet<CityActiveTrip> CityActiveTrips => Set<CityActiveTrip>();
        public DbSet<CityWeather> CityWeathers => Set<CityWeather>();
    }
}
