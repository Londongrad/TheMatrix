using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.SimulationCore.Infrastructure.Persistence.Configurations
{
    public sealed class CityActiveTripConfiguration : IEntityTypeConfiguration<CityActiveTrip>
    {
        public void Configure(EntityTypeBuilder<CityActiveTrip> builder)
        {
            builder.ToTable("CityActiveTrips");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new CityActiveTripId(x))
               .ValueGeneratedNever();

            builder.Property(x => x.CityId)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new CityId(x))
               .IsRequired();

            builder.Property(x => x.TravellerEntityId)
               .IsRequired(false);

            builder.Property(x => x.Subject)
               .HasMaxLength(CityActiveTrip.MaxSubjectLength)
               .IsRequired();

            builder.Property(x => x.Purpose)
               .HasConversion<int>()
               .IsRequired();

            builder.Property(x => x.Profile)
               .HasMaxLength(64)
               .IsRequired();

            builder.Property(x => x.MovementCapabilityIndex)
               .HasPrecision(5, 2)
               .IsRequired();

            builder.Property(x => x.UsedDynamicRoadConditions)
               .IsRequired();

            builder.Property(x => x.PlannedAtTickId)
               .IsRequired();

            builder.Property(x => x.ConditionsEffectiveTickId)
               .IsRequired(false);

            builder.Property(x => x.StartedAtSimTimeUtc)
               .IsRequired();

            builder.Property(x => x.LastAdvancedAtSimTimeUtc)
               .IsRequired();

            builder.Property(x => x.ExpectedArrivalAtSimTimeUtc)
               .IsRequired();

            builder.Property(x => x.ArrivedAtSimTimeUtc)
               .IsRequired(false);

            builder.Property(x => x.LastAdvancedTickId)
               .IsRequired();

            builder.Property(x => x.TotalDistanceMeters)
               .HasPrecision(12, 2)
               .IsRequired();

            builder.Property(x => x.PlannedTravelTimeMinutes)
               .HasPrecision(12, 2)
               .IsRequired();

            builder.Property(x => x.AdjustedTravelTimeMinutes)
               .HasPrecision(12, 2)
               .IsRequired();

            builder.Property(x => x.ProgressIndex)
               .HasPrecision(6, 4)
               .IsRequired();

            builder.Property(x => x.DistanceTravelledMeters)
               .HasPrecision(12, 2)
               .IsRequired();

            builder.Property(x => x.FromKind)
               .HasMaxLength(64)
               .IsRequired();

            builder.Property(x => x.FromEntityId)
               .IsRequired();

            builder.Property(x => x.FromDistrictId)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new DistrictId(x))
               .IsRequired();

            builder.Property(x => x.FromRoadNodeId)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new RoadNodeId(x))
               .IsRequired();

            builder.Property(x => x.FromName)
               .HasMaxLength(CityActiveTrip.MaxSubjectLength)
               .IsRequired();

            builder.Property(x => x.FromPositionX)
               .HasPrecision(6, 2)
               .IsRequired();

            builder.Property(x => x.FromPositionY)
               .HasPrecision(6, 2)
               .IsRequired();

            builder.Property(x => x.ToKind)
               .HasMaxLength(64)
               .IsRequired();

            builder.Property(x => x.ToEntityId)
               .IsRequired();

            builder.Property(x => x.ToDistrictId)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new DistrictId(x))
               .IsRequired();

            builder.Property(x => x.ToRoadNodeId)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new RoadNodeId(x))
               .IsRequired();

            builder.Property(x => x.ToName)
               .HasMaxLength(CityActiveTrip.MaxSubjectLength)
               .IsRequired();

            builder.Property(x => x.ToPositionX)
               .HasPrecision(6, 2)
               .IsRequired();

            builder.Property(x => x.ToPositionY)
               .HasPrecision(6, 2)
               .IsRequired();

            builder.Property(x => x.CurrentDistrictId)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new DistrictId(x))
               .IsRequired();

            builder.Property(x => x.CurrentRoadSegmentId)
               .HasConversion(
                    convertToProviderExpression: x => x.HasValue
                        ? x.Value.Value
                        : (Guid?)null,
                    convertFromProviderExpression: x => x.HasValue
                        ? new RoadSegmentId(x.Value)
                        : (RoadSegmentId?)null)
               .IsRequired(false);

            builder.Property(x => x.CurrentSegmentProgressIndex)
               .HasPrecision(6, 4)
               .IsRequired();

            builder.Property(x => x.CurrentPositionX)
               .HasPrecision(6, 4)
               .IsRequired();

            builder.Property(x => x.CurrentPositionY)
               .HasPrecision(6, 4)
               .IsRequired();

            builder.Property(x => x.Status)
               .HasConversion<int>()
               .IsRequired();

            builder.Ignore(x => x.DomainEvents);

            builder.HasIndex(x => x.CityId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => new { x.CityId, x.Status });
            builder.HasIndex(x => new { x.CityId, x.Status, x.StartedAtSimTimeUtc, x.Id });
            builder.HasIndex(x => x.TravellerEntityId);

            builder
               .HasOne<City>()
               .WithMany()
               .HasForeignKey(x => x.CityId)
               .OnDelete(DeleteBehavior.Cascade);

            builder
               .HasOne<District>()
               .WithMany()
               .HasForeignKey(x => x.FromDistrictId)
               .OnDelete(DeleteBehavior.Restrict);

            builder
               .HasOne<District>()
               .WithMany()
               .HasForeignKey(x => x.ToDistrictId)
               .OnDelete(DeleteBehavior.Restrict);

            builder
               .HasOne<District>()
               .WithMany()
               .HasForeignKey(x => x.CurrentDistrictId)
               .OnDelete(DeleteBehavior.Restrict);

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

            builder
               .HasOne<RoadSegment>()
               .WithMany()
               .HasForeignKey(x => x.CurrentRoadSegmentId)
               .OnDelete(DeleteBehavior.SetNull);

            builder.OwnsMany(
                navigationExpression: x => x.Segments,
                buildAction: tripSegment =>
                {
                    tripSegment.ToTable("CityActiveTripSegments");

                    tripSegment.WithOwner()
                       .HasForeignKey("CityActiveTripId");

                    tripSegment.Property<CityActiveTripId>("CityActiveTripId")
                       .HasConversion(
                            convertToProviderExpression: x => x.Value,
                            convertFromProviderExpression: x => new CityActiveTripId(x));

                    tripSegment.HasKey("CityActiveTripId", nameof(CityActiveTripSegment.Sequence));

                    tripSegment.Property(x => x.Sequence)
                       .IsRequired();

                    tripSegment.Property(x => x.RoadSegmentId)
                       .HasConversion(
                            convertToProviderExpression: x => x.Value,
                            convertFromProviderExpression: x => new RoadSegmentId(x))
                       .IsRequired();

                    tripSegment.Property(x => x.DistrictId)
                       .HasConversion(
                            convertToProviderExpression: x => x.Value,
                            convertFromProviderExpression: x => new DistrictId(x))
                       .IsRequired();

                    tripSegment.Property(x => x.FromRoadNodeId)
                       .HasConversion(
                            convertToProviderExpression: x => x.Value,
                            convertFromProviderExpression: x => new RoadNodeId(x))
                       .IsRequired();

                    tripSegment.Property(x => x.ToRoadNodeId)
                       .HasConversion(
                            convertToProviderExpression: x => x.Value,
                            convertFromProviderExpression: x => new RoadNodeId(x))
                       .IsRequired();

                    tripSegment.Property(x => x.Name)
                       .HasMaxLength(CityActiveTrip.MaxSubjectLength)
                       .IsRequired();

                    tripSegment.Property(x => x.Type)
                       .HasMaxLength(64)
                       .IsRequired();

                    tripSegment.Property(x => x.LengthMeters)
                       .HasPrecision(12, 2)
                       .IsRequired();

                    tripSegment.Property(x => x.EstimatedTraversalMinutes)
                       .HasPrecision(12, 2)
                       .IsRequired();

                    tripSegment.Property(x => x.FromPositionX)
                       .HasPrecision(6, 2)
                       .IsRequired();

                    tripSegment.Property(x => x.FromPositionY)
                       .HasPrecision(6, 2)
                       .IsRequired();

                    tripSegment.Property(x => x.ToPositionX)
                       .HasPrecision(6, 2)
                       .IsRequired();

                    tripSegment.Property(x => x.ToPositionY)
                       .HasPrecision(6, 2)
                       .IsRequired();

                    tripSegment.HasIndex("CityActiveTripId");
                    tripSegment.HasIndex(x => x.RoadSegmentId);
                    tripSegment.HasIndex(x => x.DistrictId);
                });

            builder.Navigation(x => x.Segments)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.Property<uint>("xmin")
               .HasColumnName("xmin")
               .ValueGeneratedOnAddOrUpdate()
               .IsConcurrencyToken();
        }
    }
}
