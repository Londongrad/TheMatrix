using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Simulation;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationCore.Infrastructure.Persistence
{
    public sealed class SimulationCoreDbContext(DbContextOptions<SimulationCoreDbContext> options)
        : DbContext(options)
    {
        public DbSet<SimulationClock> SimulationClocks => Set<SimulationClock>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<City> Cities => Set<City>();
        public DbSet<District> Districts => Set<District>();
        public DbSet<ResidentialBuilding> ResidentialBuildings => Set<ResidentialBuilding>();
        public DbSet<RoadNode> RoadNodes => Set<RoadNode>();
        public DbSet<RoadSegment> RoadSegments => Set<RoadSegment>();
        public DbSet<CityWeather> CityWeathers => Set<CityWeather>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SimulationCoreDbContext).Assembly);
            modelBuilder.AddOutboxMessageModel();
            base.OnModelCreating(modelBuilder);
        }
    }
}
