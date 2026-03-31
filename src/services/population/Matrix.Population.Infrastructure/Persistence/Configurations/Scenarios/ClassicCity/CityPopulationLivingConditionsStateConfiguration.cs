using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations.Scenarios.ClassicCity
{
    public sealed class CityPopulationLivingConditionsStateConfiguration
        : IEntityTypeConfiguration<CityPopulationLivingConditionsState>
    {
        public void Configure(EntityTypeBuilder<CityPopulationLivingConditionsState> builder)
        {
            builder.ToTable("CityPopulationLivingConditionsStates");

            builder.HasKey(x => x.CityId);

            builder.Property(x => x.CityId)
                .HasConversion(id => id.Value, value => CityId.From(value));

            builder.Property(x => x.FloodingIndex).HasPrecision(8, 4).IsRequired();
            builder.Property(x => x.RoadAccessibilityIndex).HasPrecision(8, 4).IsRequired();
            builder.Property(x => x.PowerCoverageIndex).HasPrecision(8, 4).IsRequired();
            builder.Property(x => x.UtilityContinuityIndex).HasPrecision(8, 4).IsRequired();
            builder.Property(x => x.HeatingCoverageIndex).HasPrecision(8, 4).IsRequired();
            builder.Property(x => x.WaterCoverageIndex).HasPrecision(8, 4).IsRequired();
            builder.Property(x => x.SanitationCoverageIndex).HasPrecision(8, 4).IsRequired();
            builder.Property(x => x.EffectiveTickId).IsRequired();
            builder.Property(x => x.EffectiveAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();

            builder.HasIndex(x => x.UpdatedAtUtc);
            builder.HasIndex(x => x.EffectiveTickId);
        }
    }
}
