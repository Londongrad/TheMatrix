using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations.Scenarios.ClassicCity
{
    public sealed class CityPopulationEmployerFinancialStressStateConfiguration
        : IEntityTypeConfiguration<CityPopulationEmployerFinancialStressState>
    {
        public void Configure(EntityTypeBuilder<CityPopulationEmployerFinancialStressState> builder)
        {
            builder.ToTable("CityPopulationEmployerFinancialStressStates");

            builder.HasKey(x => new
            {
                x.CityId,
                x.WorkplaceId
            });

            builder.Property(x => x.CityId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => CityId.From(value));

            builder.Property(x => x.WorkplaceId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => WorkplaceId.From(value));

            builder.Property(x => x.RecentGrossPayrollAmount)
               .HasPrecision(
                    precision: 18,
                    scale: 2)
               .IsRequired();

            builder.Property(x => x.CurrentBalanceAmount)
               .HasPrecision(
                    precision: 18,
                    scale: 2)
               .IsRequired();

            builder.Property(x => x.DistressScore)
               .HasPrecision(
                    precision: 8,
                    scale: 4)
               .IsRequired();

            builder.Property(x => x.HasHiringFreeze)
               .IsRequired();

            builder.Property(x => x.HasLayoffPressure)
               .IsRequired();

            builder.Property(x => x.LastEvaluatedAtUtc)
               .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
               .IsRequired();

            builder.HasIndex(x => x.UpdatedAtUtc);
        }
    }
}
