using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations
{
    public sealed class CityPopulationWeatherExposureStateConfiguration : IEntityTypeConfiguration<CityPopulationWeatherExposureState>
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
               .HasPrecision(10, 2);

            builder.Property(x => x.CurrentHumidityPercent)
               .HasPrecision(10, 2);

            builder.Property(x => x.CurrentWindSpeedKph)
               .HasPrecision(10, 2);

            builder.Property(x => x.CurrentCloudCoveragePercent)
               .HasPrecision(10, 2);

            builder.Property(x => x.CurrentPressureHpa)
               .HasPrecision(10, 2);

            builder.Property(x => x.PreviousTemperatureC)
               .HasPrecision(10, 2);

            builder.Property(x => x.PreviousHumidityPercent)
               .HasPrecision(10, 2);

            builder.Property(x => x.PreviousWindSpeedKph)
               .HasPrecision(10, 2);

            builder.Property(x => x.PreviousCloudCoveragePercent)
               .HasPrecision(10, 2);

            builder.Property(x => x.PreviousPressureHpa)
               .HasPrecision(10, 2);

            builder.Property(x => x.RecoverySourceTemperatureC)
               .HasPrecision(10, 2);

            builder.Property(x => x.RecoverySourceHumidityPercent)
               .HasPrecision(10, 2);

            builder.Property(x => x.RecoverySourceWindSpeedKph)
               .HasPrecision(10, 2);

            builder.Property(x => x.RecoverySourceCloudCoveragePercent)
               .HasPrecision(10, 2);

            builder.Property(x => x.RecoverySourcePressureHpa)
               .HasPrecision(10, 2);

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
