using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations
{
    public sealed class
        CityPopulationWeatherExposureStateConfiguration : IEntityTypeConfiguration<CityPopulationWeatherExposureState>
    {
        public void Configure(EntityTypeBuilder<CityPopulationWeatherExposureState> builder)
        {
            builder.ToTable("CityPopulationWeatherExposureStates");

            builder.HasKey(x => x.CityId);

            builder.Property(x => x.CityId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => CityId.From(value));

            builder.Property(x => x.CurrentTemperatureC)
               .HasPrecision(
                    precision: 10,
                    scale: 2);

            builder.Property(x => x.CurrentHumidityPercent)
               .HasPrecision(
                    precision: 10,
                    scale: 2);

            builder.Property(x => x.CurrentWindSpeedKph)
               .HasPrecision(
                    precision: 10,
                    scale: 2);

            builder.Property(x => x.CurrentCloudCoveragePercent)
               .HasPrecision(
                    precision: 10,
                    scale: 2);

            builder.Property(x => x.CurrentPressureHpa)
               .HasPrecision(
                    precision: 10,
                    scale: 2);

            builder.Property(x => x.PreviousTemperatureC)
               .HasPrecision(
                    precision: 10,
                    scale: 2);

            builder.Property(x => x.PreviousHumidityPercent)
               .HasPrecision(
                    precision: 10,
                    scale: 2);

            builder.Property(x => x.PreviousWindSpeedKph)
               .HasPrecision(
                    precision: 10,
                    scale: 2);

            builder.Property(x => x.PreviousCloudCoveragePercent)
               .HasPrecision(
                    precision: 10,
                    scale: 2);

            builder.Property(x => x.PreviousPressureHpa)
               .HasPrecision(
                    precision: 10,
                    scale: 2);

            builder.Property(x => x.RecoverySourceTemperatureC)
               .HasPrecision(
                    precision: 10,
                    scale: 2);

            builder.Property(x => x.RecoverySourceHumidityPercent)
               .HasPrecision(
                    precision: 10,
                    scale: 2);

            builder.Property(x => x.RecoverySourceWindSpeedKph)
               .HasPrecision(
                    precision: 10,
                    scale: 2);

            builder.Property(x => x.RecoverySourceCloudCoveragePercent)
               .HasPrecision(
                    precision: 10,
                    scale: 2);

            builder.Property(x => x.RecoverySourcePressureHpa)
               .HasPrecision(
                    precision: 10,
                    scale: 2);

            builder.Property(x => x.CurrentWeatherEffectiveAtSimTimeUtc)
               .IsRequired();

            builder.Property(x => x.LastWeatherOccurredOnUtc)
               .IsRequired();

            builder.Property(x => x.LastExposureProcessedAtSimTimeUtc)
               .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
               .IsRequired();

            builder.HasIndex(x => x.UpdatedAtUtc);
            builder.HasIndex(x => x.LastExposureProcessedAtSimTimeUtc);
        }
    }
}
