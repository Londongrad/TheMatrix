using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations.Scenarios.ClassicCity
{
    public sealed class CityPopulationServiceQualityStateConfiguration
        : IEntityTypeConfiguration<CityPopulationServiceQualityState>
    {
        public void Configure(EntityTypeBuilder<CityPopulationServiceQualityState> builder)
        {
            builder.ToTable("CityPopulationServiceQualityStates");

            builder.HasKey(x => x.CityId);

            builder.Property(x => x.CityId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => CityId.From(value));

            builder.Property(x => x.HealthcareQualityIndex)
               .HasPrecision(
                    precision: 8,
                    scale: 4)
               .IsRequired();

            builder.Property(x => x.HousingSupportIndex)
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
