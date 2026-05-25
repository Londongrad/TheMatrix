using Matrix.Economy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Economy.Infrastructure.Persistence.Configurations
{
    public sealed class CityEconomyCostProfileStateConfiguration
        : IEntityTypeConfiguration<CityEconomyCostProfileState>
    {
        public void Configure(EntityTypeBuilder<CityEconomyCostProfileState> builder)
        {
            builder.ToTable("CityEconomyCostProfileStates");

            builder.HasKey(x => x.CityId);

            builder.Property(x => x.CityId)
               .HasColumnName("city_id");
            builder.Property(x => x.BaseWageMultiplier)
               .HasColumnName("base_wage_multiplier")
               .HasPrecision(
                    precision: 8,
                    scale: 4)
               .IsRequired();
            builder.Property(x => x.BaseRetailPriceMultiplier)
               .HasColumnName("base_retail_price_multiplier")
               .HasPrecision(
                    precision: 8,
                    scale: 4)
               .IsRequired();
            builder.Property(x => x.BaseHousingCostMultiplier)
               .HasColumnName("base_housing_cost_multiplier")
               .HasPrecision(
                    precision: 8,
                    scale: 4)
               .IsRequired();
            builder.Property(x => x.BaseUtilityCostMultiplier)
               .HasColumnName("base_utility_cost_multiplier")
               .HasPrecision(
                    precision: 8,
                    scale: 4)
               .IsRequired();
            builder.Property(x => x.WageMultiplier)
               .HasColumnName("wage_multiplier")
               .HasPrecision(
                    precision: 8,
                    scale: 4)
               .IsRequired();
            builder.Property(x => x.RetailPriceMultiplier)
               .HasColumnName("retail_price_multiplier")
               .HasPrecision(
                    precision: 8,
                    scale: 4)
               .IsRequired();
            builder.Property(x => x.HousingCostMultiplier)
               .HasColumnName("housing_cost_multiplier")
               .HasPrecision(
                    precision: 8,
                    scale: 4)
               .IsRequired();
            builder.Property(x => x.UtilityCostMultiplier)
               .HasColumnName("utility_cost_multiplier")
               .HasPrecision(
                    precision: 8,
                    scale: 4)
               .IsRequired();
            builder.Property(x => x.CostOfLivingIndex)
               .HasColumnName("cost_of_living_index")
               .HasPrecision(
                    precision: 8,
                    scale: 4)
               .IsRequired();
            builder.Property(x => x.AffordabilityIndex)
               .HasColumnName("affordability_index")
               .HasPrecision(
                    precision: 8,
                    scale: 4)
               .IsRequired();
            builder.Property(x => x.LastEvaluatedAtUtc)
               .HasColumnName("last_evaluated_at_utc")
               .IsRequired();
            builder.Property(x => x.UpdatedAtUtc)
               .HasColumnName("updated_at_utc")
               .IsRequired();
        }
    }
}
