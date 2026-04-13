using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations.Scenarios.ClassicCity
{
    public sealed class CityPopulationSummaryProjectionConfiguration
        : IEntityTypeConfiguration<CityPopulationSummaryProjection>
    {
        public void Configure(EntityTypeBuilder<CityPopulationSummaryProjection> builder)
        {
            builder.ToTable("CityPopulationSummaryProjections");

            builder.HasKey(x => x.CityId);

            builder.Property(x => x.CityId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => CityId.From(value));

            builder.Property(x => x.CurrentDate)
               .HasConversion(
                    convertToProviderExpression: date => date.ToDateTime(TimeOnly.MinValue),
                    convertFromProviderExpression: value => DateOnly.FromDateTime(value))
               .HasColumnType("date")
               .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
               .IsRequired();

            builder.Property(x => x.AverageHealth)
               .HasPrecision(
                    precision: 10,
                    scale: 2);

            builder.Property(x => x.AverageHappiness)
               .HasPrecision(
                    precision: 10,
                    scale: 2);

            builder.Property(x => x.AverageEnergy)
               .HasPrecision(
                    precision: 10,
                    scale: 2);

            builder.Property(x => x.AverageStress)
               .HasPrecision(
                    precision: 10,
                    scale: 2);

            builder.Property(x => x.AverageSocialNeed)
               .HasPrecision(
                    precision: 10,
                    scale: 2);

            builder.Property(x => x.WorkforceAttendanceIndex)
               .HasPrecision(
                    precision: 10,
                    scale: 4);

            builder.Property(x => x.WorkforceCommuteAccessibilityIndex)
               .HasPrecision(
                    precision: 10,
                    scale: 4);

            builder.Property(x => x.WorkforceProductivityIndex)
               .HasPrecision(
                    precision: 10,
                    scale: 4);

            builder.Property(x => x.StudentCommuteAccessibilityIndex)
               .HasPrecision(
                    precision: 10,
                    scale: 4);

            builder.Property(x => x.StudentAttendanceIndex)
               .HasPrecision(
                    precision: 10,
                    scale: 4);

            builder.HasIndex(x => x.UpdatedAtUtc);
        }
    }
}
