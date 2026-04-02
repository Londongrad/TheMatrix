using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations.Scenarios.ClassicCity
{
    public sealed class CityPopulationAnchorCatalogItemConfiguration
        : IEntityTypeConfiguration<CityPopulationAnchorCatalogItem>
    {
        public void Configure(EntityTypeBuilder<CityPopulationAnchorCatalogItem> builder)
        {
            builder.ToTable("CityPopulationAnchorCatalogItems");

            builder.HasKey(x => new
            {
                x.CityId,
                x.CityAnchorId
            });

            builder.Property(x => x.CityId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => CityId.From(value));

            builder.Property(x => x.CityAnchorId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => CityAnchorId.From(value));

            builder.Property(x => x.DistrictId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => DistrictId.From(value));

            builder.Property(x => x.AccessRoadNodeId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => RoadNodeId.From(value));

            builder.Property(x => x.Name)
               .HasMaxLength(200)
               .IsRequired();

            builder.Property(x => x.Type)
               .HasConversion<string>()
               .HasMaxLength(32)
               .IsRequired();

            builder.Property(x => x.Capacity)
               .IsRequired();

            builder.Property(x => x.PositionX)
               .HasPrecision(
                    precision: 10,
                    scale: 4)
               .IsRequired();

            builder.Property(x => x.PositionY)
               .HasPrecision(
                    precision: 10,
                    scale: 4)
               .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
               .IsRequired();

            builder.HasIndex(x => new
            {
                x.CityId,
                x.Type
            });

            builder.HasIndex(x => x.DistrictId);
        }
    }
}
