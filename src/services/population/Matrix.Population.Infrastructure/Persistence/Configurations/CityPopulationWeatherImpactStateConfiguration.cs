using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations
{
    public sealed class CityPopulationWeatherImpactStateConfiguration : IEntityTypeConfiguration<CityPopulationWeatherImpactState>
    {
        public void Configure(EntityTypeBuilder<CityPopulationWeatherImpactState> builder)
        {
            builder.ToTable("CityPopulationWeatherImpactStates");

            builder.HasKey(x => x.CityId);

            builder.Property(x => x.CityId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => CityId.From(value));

            builder.Property(x => x.LastAppliedAtSimTimeUtc)
               .IsRequired();

            builder.Property(x => x.LastAppliedOccurredOnUtc)
               .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
               .IsRequired();

            builder.HasIndex(x => x.UpdatedAtUtc);
        }
    }
}
