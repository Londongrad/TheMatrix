using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations.Scenarios.ClassicCity
{
    public sealed class CityPopulationDailySummarySnapshotConfiguration
        : IEntityTypeConfiguration<CityPopulationDailySummarySnapshot>
    {
        public void Configure(EntityTypeBuilder<CityPopulationDailySummarySnapshot> builder)
        {
            builder.ToTable("CityPopulationDailySummarySnapshots");

            builder.HasKey(x => new { x.CityId, x.SnapshotDate });

            builder.Property(x => x.CityId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => CityId.From(value));

            builder.Property(x => x.SnapshotDate)
               .HasConversion(
                    convertToProviderExpression: date => date.ToDateTime(TimeOnly.MinValue),
                    convertFromProviderExpression: value => DateOnly.FromDateTime(value))
               .HasColumnType("date")
               .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
               .IsRequired();

            builder.Property(x => x.AverageHealth)
               .HasPrecision(10, 2);

            builder.Property(x => x.AverageHappiness)
               .HasPrecision(10, 2);

            builder.Property(x => x.AverageEnergy)
               .HasPrecision(10, 2);

            builder.Property(x => x.AverageStress)
               .HasPrecision(10, 2);

            builder.Property(x => x.AverageSocialNeed)
               .HasPrecision(10, 2);

            builder.HasIndex(x => new { x.CityId, x.UpdatedAtUtc });
        }
    }
}
