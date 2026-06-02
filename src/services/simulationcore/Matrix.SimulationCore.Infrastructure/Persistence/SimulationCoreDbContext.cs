using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Persistence;
using Matrix.SimulationCore.Domain.Simulation;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationCore.Infrastructure.Persistence
{
    public sealed partial class SimulationCoreDbContext(DbContextOptions<SimulationCoreDbContext> options)
        : DbContext(options)
    {
        public DbSet<SimulationInstance> SimulationInstances => Set<SimulationInstance>();
        public DbSet<SimulationClock> SimulationClocks => Set<SimulationClock>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SimulationCoreDbContext).Assembly);
            modelBuilder.AddOutboxMessageModel();
            base.OnModelCreating(modelBuilder);
        }
    }
}
