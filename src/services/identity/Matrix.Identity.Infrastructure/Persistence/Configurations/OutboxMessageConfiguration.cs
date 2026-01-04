using Matrix.Identity.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Identity.Infrastructure.Persistence.Configurations
{
    internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .ValueGeneratedNever();

            builder.Property(x => x.OccurredOnUtc)
               .IsRequired();

            builder.Property(x => x.Type)
               .HasMaxLength(256)
               .IsRequired();

            builder.Property(x => x.PayloadJson)
               .IsRequired();

            builder.Property(x => x.ProcessedOnUtc)
               .IsRequired(false);

            builder.Property(x => x.Error)
               .HasMaxLength(1024)
               .IsRequired(false);

            builder.HasIndex(x => x.ProcessedOnUtc);
        }
    }
}
