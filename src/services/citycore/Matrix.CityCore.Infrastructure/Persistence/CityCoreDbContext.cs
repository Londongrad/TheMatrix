using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Persistence;
using Matrix.CityCore.Domain.Simulation;
using Microsoft.EntityFrameworkCore;

namespace Matrix.CityCore.Infrastructure.Persistence
{
    public sealed class CityCoreDbContext(DbContextOptions<CityCoreDbContext> options)
        : DbContext(options)
    {
        public DbSet<SimulationClock> SimulationClocks => Set<SimulationClock>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CityCoreDbContext).Assembly);
            modelBuilder.AddOutboxMessageModel();
            base.OnModelCreating(modelBuilder);
        }
    }
}
