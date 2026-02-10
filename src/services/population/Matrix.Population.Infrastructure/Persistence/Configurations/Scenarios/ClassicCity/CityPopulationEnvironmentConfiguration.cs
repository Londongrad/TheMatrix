using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations
{
    public sealed class CityPopulationEnvironmentConfiguration : IEntityTypeConfiguration<CityPopulationEnvironment>
    {
        public void Configure(EntityTypeBuilder<CityPopulationEnvironment> builder)
        {
            builder.ToTable("CityPopulationEnvironments");

            builder.HasKey(x => x.CityId);

            builder.Property(x => x.CityId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => CityId.From(value));

            builder.Property(x => x.ClimateZone)
               .IsRequired();

            builder.Property(x => x.Hemisphere)
               .IsRequired();

            builder.Property(x => x.UtcOffsetMinutes)
               .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
               .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
               .IsRequired();

            builder.HasIndex(x => x.UpdatedAtUtc);
        }
    }
}
