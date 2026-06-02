using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity.Persistence.Configurations
{
    public sealed class RoadSegmentConfiguration : IEntityTypeConfiguration<RoadSegment>
    {
        public void Configure(EntityTypeBuilder<RoadSegment> builder)
        {
            builder.ToTable("RoadSegments");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new RoadSegmentId(x))
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

            builder.Property(x => x.FromRoadNodeId)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new RoadNodeId(x))
               .IsRequired();

            builder.Property(x => x.ToRoadNodeId)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new RoadNodeId(x))
               .IsRequired();

            builder.Property(x => x.Name)
               .HasMaxLength(RoadSegment.MaxNameLength)
               .IsRequired();

            builder.Property(x => x.Type)
               .HasConversion<int>()
               .IsRequired();

            builder.Property(x => x.LengthMeters)
               .HasPrecision(
                    precision: 10,
                    scale: 2)
               .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
               .IsRequired();

            builder.Ignore(x => x.DomainEvents);

            builder.HasIndex(x => x.CityId);
            builder.HasIndex(x => x.DistrictId);
            builder.HasIndex(x => x.FromRoadNodeId);
            builder.HasIndex(x => x.ToRoadNodeId);
            builder.HasIndex(x => new
            {
                x.CityId,
                x.Type,
                x.Name
            });

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
               .HasForeignKey(x => x.FromRoadNodeId)
               .OnDelete(DeleteBehavior.Restrict);

            builder
               .HasOne<RoadNode>()
               .WithMany()
               .HasForeignKey(x => x.ToRoadNodeId)
               .OnDelete(DeleteBehavior.Restrict);

            builder.Property<uint>("xmin")
               .HasColumnName("xmin")
               .ValueGeneratedOnAddOrUpdate()
               .IsConcurrencyToken();
        }
    }
}
