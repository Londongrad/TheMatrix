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
               .HasColumnType("text")
               .HasMaxLength(5000)
               .IsRequired();

            builder.Property(x => x.ProcessedOnUtc)
               .IsRequired(false);

            builder.Property(x => x.Error)
               .HasColumnType("text")
               .HasMaxLength(1024)
               .IsRequired(false);

            // lease/retry поля
            builder.Property(x => x.LockToken)
               .IsRequired(false);

            builder.Property(x => x.LockedUntilUtc)
               .IsRequired(false);

            builder.Property(x => x.AttemptCount)
               .HasDefaultValue(0)
               .IsRequired();

            builder.Property(x => x.NextAttemptOnUtc)
               .IsRequired(false);

            builder.Property(x => x.LastAttemptOnUtc)
               .IsRequired(false);

            // Индекс под "pending"
            builder.HasIndex(x => new
                {
                    x.ProcessedOnUtc,
                    x.LockedUntilUtc,
                    x.NextAttemptOnUtc,
                    x.OccurredOnUtc
                })
               .HasFilter("\"ProcessedOnUtc\" IS NULL"); // 👈 partial index для Postgres
        }
    }
}
