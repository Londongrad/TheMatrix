using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Resources.Infrastructure.Persistence
{
    public sealed partial class ResourcesDbContext(DbContextOptions<ResourcesDbContext> options)
        : DbContext(options)
    {
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.AddOutboxMessageModel();
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ResourcesDbContext).Assembly);
        }
    }
}
