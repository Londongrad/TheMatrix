using Matrix.Population.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations
{
    public sealed class ProcessedIntegrationMessageConfiguration : IEntityTypeConfiguration<ProcessedIntegrationMessage>
    {
        public void Configure(EntityTypeBuilder<ProcessedIntegrationMessage> builder)
        {
            builder.ToTable("ProcessedIntegrationMessages");

            builder.HasKey(x => new
            {
                x.Consumer,
                x.MessageId
            });

            builder.Property(x => x.Consumer)
               .HasMaxLength(128)
               .IsRequired();

            builder.Property(x => x.MessageId)
               .IsRequired();

            builder.Property(x => x.ProcessedAtUtc)
               .IsRequired();

            builder.HasIndex(x => x.ProcessedAtUtc);
        }
    }
}
