using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations.Scenarios.ClassicCity
{
    public sealed class CityPopulationArchiveStateConfiguration : IEntityTypeConfiguration<CityPopulationArchiveState>
    {
        public void Configure(EntityTypeBuilder<CityPopulationArchiveState> builder)
        {
            builder.ToTable("CityPopulationArchiveStates");

            builder.HasKey(x => x.CityId);

            builder.Property(x => x.CityId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => CityId.From(value));

            builder.Property(x => x.ArchivedAtUtc)
               .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
               .IsRequired();

            builder.HasIndex(x => x.ArchivedAtUtc);
        }
    }
}
