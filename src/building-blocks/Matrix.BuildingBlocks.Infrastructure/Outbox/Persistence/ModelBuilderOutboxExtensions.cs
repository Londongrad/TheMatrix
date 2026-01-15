using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Microsoft.EntityFrameworkCore;

namespace Matrix.BuildingBlocks.Infrastructure.Outbox.Persistence
{
    public static class ModelBuilderOutboxExtensions
    {
        public static ModelBuilder AddOutboxMessageModel(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxMessage>()
               .ConfigureOutboxMessage();

            return modelBuilder;
        }
    }
}
