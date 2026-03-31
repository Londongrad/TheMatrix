using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations.Scenarios.ClassicCity
{
    public sealed class CityPopulationEssentialsStateConfiguration
        : IEntityTypeConfiguration<CityPopulationEssentialsState>
    {
        public void Configure(EntityTypeBuilder<CityPopulationEssentialsState> builder)
        {
            builder.ToTable("CityPopulationEssentialsStates");

            builder.HasKey(x => x.CityId);

            builder.Property(x => x.CityId)
                .HasConversion(id => id.Value, value => CityId.From(value));

            builder.Property(x => x.SupplyStressIndex).HasPrecision(8, 4).IsRequired();
            builder.Property(x => x.EmergencyRationingEnabled).IsRequired();
            builder.Property(x => x.FoodStockLevelIndex).HasPrecision(8, 4).IsRequired();
            builder.Property(x => x.FoodShortageRiskIndex).HasPrecision(8, 4).IsRequired();
            builder.Property(x => x.MedicineStockLevelIndex).HasPrecision(8, 4).IsRequired();
            builder.Property(x => x.MedicineShortageRiskIndex).HasPrecision(8, 4).IsRequired();
            builder.Property(x => x.EmergencyWaterStockLevelIndex).HasPrecision(8, 4).IsRequired();
            builder.Property(x => x.EmergencyWaterShortageRiskIndex).HasPrecision(8, 4).IsRequired();
            builder.Property(x => x.EffectiveTickId).IsRequired();
            builder.Property(x => x.EffectiveAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();

            builder.HasIndex(x => x.UpdatedAtUtc);
            builder.HasIndex(x => x.EffectiveTickId);
        }
    }
}
