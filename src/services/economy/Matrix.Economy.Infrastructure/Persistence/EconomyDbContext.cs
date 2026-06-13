using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Persistence
{
    public partial class EconomyDbContext(DbContextOptions<EconomyDbContext> options)
        : DbContext(options)
    {
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.AddOutboxMessageModel();
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(EconomyDbContext).Assembly);
        }
    }
}
