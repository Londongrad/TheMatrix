using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations.Scenarios.ClassicCity
{
    public sealed class CityPopulationPendingWeatherImpactConfiguration
        : IEntityTypeConfiguration<CityPopulationPendingWeatherImpact>
    {
        public void Configure(EntityTypeBuilder<CityPopulationPendingWeatherImpact> builder)
        {
            builder.ToTable("CityPopulationPendingWeatherImpacts");

            builder.HasKey(x => x.ImpactId);

            builder.Property(x => x.CityId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => CityId.From(value))
               .IsRequired();

            builder.Property(x => x.CurrentDate)
               .HasColumnType("date")
               .IsRequired();

            builder.Property(x => x.PreviousType).HasConversion<int>().IsRequired();
            builder.Property(x => x.PreviousSeverity).HasConversion<int>().IsRequired();
            builder.Property(x => x.PreviousPrecipitationKind).HasConversion<int>().IsRequired();
            builder.Property(x => x.PreviousTemperatureC).HasPrecision(8, 2).IsRequired();
            builder.Property(x => x.PreviousHumidityPercent).HasPrecision(8, 2).IsRequired();
            builder.Property(x => x.PreviousWindSpeedKph).HasPrecision(8, 2).IsRequired();
            builder.Property(x => x.PreviousCloudCoveragePercent).HasPrecision(8, 2).IsRequired();
            builder.Property(x => x.PreviousPressureHpa).HasPrecision(8, 2).IsRequired();

            builder.Property(x => x.CurrentType).HasConversion<int>().IsRequired();
            builder.Property(x => x.CurrentSeverity).HasConversion<int>().IsRequired();
            builder.Property(x => x.CurrentPrecipitationKind).HasConversion<int>().IsRequired();
            builder.Property(x => x.CurrentTemperatureC).HasPrecision(8, 2).IsRequired();
            builder.Property(x => x.CurrentHumidityPercent).HasPrecision(8, 2).IsRequired();
            builder.Property(x => x.CurrentWindSpeedKph).HasPrecision(8, 2).IsRequired();
            builder.Property(x => x.CurrentCloudCoveragePercent).HasPrecision(8, 2).IsRequired();
            builder.Property(x => x.CurrentPressureHpa).HasPrecision(8, 2).IsRequired();

            builder.Property(x => x.EnvironmentClimateZone).HasConversion<int?>();
            builder.Property(x => x.EnvironmentHemisphere).HasConversion<int?>();
            builder.Property(x => x.EnvironmentUtcOffsetMinutes);
            builder.Property(x => x.OccurredAtUtc).IsRequired();

            builder.Ignore(x => x.PreviousWeather);
            builder.Ignore(x => x.CurrentWeather);
            builder.Ignore(x => x.Environment);

            builder.HasIndex(x => new
                {
                    x.CityId,
                    x.OccurredAtUtc
                })
               .HasDatabaseName("IX_CityPopulationPendingWeatherImpacts_CityId_OccurredAtUtc");
        }
    }
}
