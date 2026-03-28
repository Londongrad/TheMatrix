using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Persistence;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationSystems.Infrastructure.Persistence
{
    public class SimulationSystemsDbContext(DbContextOptions<SimulationSystemsDbContext> options)
        : DbContext(options)
    {
        public DbSet<CityEnvironmentalConditionState> CityEnvironmentalConditions => Set<CityEnvironmentalConditionState>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.AddOutboxMessageModel();
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SimulationSystemsDbContext).Assembly);
        }
    }
}
