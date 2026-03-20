using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations.Scenarios.ClassicCity
{
    public sealed class CityPopulationHouseholdFinancialStressStateConfiguration
        : IEntityTypeConfiguration<CityPopulationHouseholdFinancialStressState>
    {
        public void Configure(EntityTypeBuilder<CityPopulationHouseholdFinancialStressState> builder)
        {
            builder.ToTable("CityPopulationHouseholdFinancialStressStates");

            builder.HasKey(x => new
            {
                x.CityId,
                x.HouseholdId
            });

            builder.Property(x => x.CityId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => CityId.From(value));

            builder.Property(x => x.HouseholdId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => HouseholdId.From(value));

            builder.Property(x => x.OverdueObligationCount)
               .IsRequired();

            builder.Property(x => x.OverdueRentCount)
               .IsRequired();

            builder.Property(x => x.OverdueUtilityCount)
               .IsRequired();

            builder.Property(x => x.ArrearsObligationCount)
               .IsRequired();

            builder.Property(x => x.ServiceCutoffCount)
               .IsRequired();

            builder.Property(x => x.EvictionNoticeCount)
               .IsRequired();

            builder.Property(x => x.EvictionEligibleCount)
               .IsRequired();

            builder.Property(x => x.OldestOverdueAgeDays)
               .IsRequired();

            builder.Property(x => x.TotalOverdueAmount)
               .HasPrecision(
                    precision: 18,
                    scale: 2)
               .IsRequired();

            builder.Property(x => x.DistressScore)
               .HasPrecision(
                    precision: 8,
                    scale: 4)
               .IsRequired();

            builder.Property(x => x.LastEvaluatedAtUtc)
               .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
               .IsRequired();

            builder.HasIndex(x => x.UpdatedAtUtc);
        }
    }
}
