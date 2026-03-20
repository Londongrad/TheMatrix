using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations.Scenarios.ClassicCity
{
    public sealed class CityPopulationCostOfLivingStateConfiguration
        : IEntityTypeConfiguration<CityPopulationCostOfLivingState>
    {
        public void Configure(EntityTypeBuilder<CityPopulationCostOfLivingState> builder)
        {
            builder.ToTable("CityPopulationCostOfLivingStates");

            builder.HasKey(x => x.CityId);

            builder.Property(x => x.CityId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => CityId.From(value));

            builder.Property(x => x.WageMultiplier)
               .HasPrecision(8, 4)
               .IsRequired();

            builder.Property(x => x.RetailPriceMultiplier)
               .HasPrecision(8, 4)
               .IsRequired();

            builder.Property(x => x.HousingCostMultiplier)
               .HasPrecision(8, 4)
               .IsRequired();

            builder.Property(x => x.UtilityCostMultiplier)
               .HasPrecision(8, 4)
               .IsRequired();

            builder.Property(x => x.CostOfLivingIndex)
               .HasPrecision(8, 4)
               .IsRequired();

            builder.Property(x => x.AffordabilityIndex)
               .HasPrecision(8, 4)
               .IsRequired();

            builder.Property(x => x.LastEvaluatedAtUtc)
               .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
               .IsRequired();

            builder.HasIndex(x => x.UpdatedAtUtc);
        }
    }
}
