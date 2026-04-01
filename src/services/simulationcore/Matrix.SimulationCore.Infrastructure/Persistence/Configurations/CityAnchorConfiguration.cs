using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.SimulationCore.Infrastructure.Persistence.Configurations
{
    public sealed class CityAnchorConfiguration : IEntityTypeConfiguration<CityAnchor>
    {
        public void Configure(EntityTypeBuilder<CityAnchor> builder)
        {
            builder.ToTable("CityAnchors");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new CityAnchorId(x))
               .ValueGeneratedNever();

            builder.Property(x => x.CityId)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new CityId(x))
               .IsRequired();

            builder.Property(x => x.DistrictId)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new DistrictId(x))
               .IsRequired();

            builder.Property(x => x.AccessRoadNodeId)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new RoadNodeId(x))
               .IsRequired();

            builder.Property(x => x.Name)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new CityAnchorName(x))
               .HasMaxLength(CityAnchorName.MaxLength)
               .IsRequired();

            builder.Property(x => x.Type)
               .HasConversion<int>()
               .IsRequired();

            builder.Property(x => x.Capacity)
               .IsRequired();

            builder.Property(x => x.PositionX)
               .HasPrecision(9, 3)
               .IsRequired();

            builder.Property(x => x.PositionY)
               .HasPrecision(9, 3)
               .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
               .IsRequired();

            builder.Ignore(x => x.DomainEvents);

            builder.HasIndex(x => x.CityId);
            builder.HasIndex(x => x.DistrictId);
            builder.HasIndex(x => x.AccessRoadNodeId);
            builder.HasIndex(x => new { x.CityId, x.Type });

            builder
               .HasOne<City>()
               .WithMany()
               .HasForeignKey(x => x.CityId)
               .OnDelete(DeleteBehavior.Cascade);

            builder
               .HasOne<District>()
               .WithMany()
               .HasForeignKey(x => x.DistrictId)
               .OnDelete(DeleteBehavior.Cascade);

            builder
               .HasOne<RoadNode>()
               .WithMany()
               .HasForeignKey(x => x.AccessRoadNodeId)
               .OnDelete(DeleteBehavior.Cascade);

            builder.Property<uint>("xmin")
               .HasColumnName("xmin")
               .ValueGeneratedOnAddOrUpdate()
               .IsConcurrencyToken();
        }
    }
}
