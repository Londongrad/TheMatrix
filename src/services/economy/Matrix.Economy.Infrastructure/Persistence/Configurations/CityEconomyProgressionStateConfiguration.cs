using Matrix.Economy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Economy.Infrastructure.Persistence.Configurations
{
    public sealed class CityEconomyProgressionStateConfiguration
        : IEntityTypeConfiguration<CityEconomyProgressionState>
    {
        public void Configure(EntityTypeBuilder<CityEconomyProgressionState> builder)
        {
            builder.ToTable("CityEconomyProgressionStates");

            builder.HasKey(x => x.CityId);

            builder.Property(x => x.CityId)
               .HasColumnName("city_id");

            builder.Property(x => x.LastCompletedTickId)
               .HasColumnName("last_completed_tick_id")
               .IsRequired();

            builder.Property(x => x.LastProcessedDate)
               .HasColumnName("last_processed_date")
               .HasConversion(
                    convertToProviderExpression: value => value.ToDateTime(TimeOnly.MinValue),
                    convertFromProviderExpression: value => DateOnly.FromDateTime(value))
               .HasColumnType("date")
               .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
               .HasColumnName("updated_at_utc")
               .IsRequired();
        }
    }
}
