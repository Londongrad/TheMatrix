using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Persistence;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Simulation;
using Matrix.CityCore.Domain.Topology;
using Matrix.CityCore.Domain.Weather;
using Microsoft.EntityFrameworkCore;

namespace Matrix.CityCore.Infrastructure.Persistence
{
    public sealed class CityCoreDbContext(DbContextOptions<CityCoreDbContext> options)
        : DbContext(options)
    {
        public DbSet<SimulationClock> SimulationClocks => Set<SimulationClock>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<City> Cities => Set<City>();
        public DbSet<District> Districts => Set<District>();
        public DbSet<ResidentialBuilding> ResidentialBuildings => Set<ResidentialBuilding>();
        public DbSet<CityWeather> CityWeathers => Set<CityWeather>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CityCoreDbContext).Assembly);
            modelBuilder.AddOutboxMessageModel();
            base.OnModelCreating(modelBuilder);
        }
    }
}
