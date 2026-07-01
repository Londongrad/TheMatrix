using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Healthcare.Infrastructure.Persistence.Configurations
{
    public sealed class CareFacilityConfiguration : IEntityTypeConfiguration<CareFacility>
    {
        public void Configure(EntityTypeBuilder<CareFacility> builder)
        {
            builder.ToTable(
                "healthcare_care_facilities",
                table => table.HasCheckConstraint(
                    "ck_healthcare_care_facilities_daily_capacity_positive",
                    "daily_patient_capacity > 0"));

            builder.HasKey(facility => facility.Id);

            builder.Property(facility => facility.Id)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => new CareFacilityId(value))
               .HasColumnName("care_facility_id");

            builder.Property(facility => facility.SimulationHostId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => new SimulationHostId(value))
               .HasColumnName("simulation_host_id")
               .IsRequired();

            builder.Property(facility => facility.Name)
               .HasMaxLength(CareFacility.MaxNameLength)
               .HasColumnName("name")
               .IsRequired();

            builder.Property(facility => facility.Kind)
               .HasConversion(
                    convertToProviderExpression: kind => kind.Value,
                    convertFromProviderExpression: value => new CareFacilityKindKey(value))
               .HasMaxLength(CareFacilityKindKey.MaxLength)
               .HasColumnName("kind")
               .IsRequired();

            builder.Property(facility => facility.LocationAnchorId)
               .HasConversion(
                    convertToProviderExpression: id => id.HasValue
                        ? id.Value.Value
                        : (Guid?)null,
                    convertFromProviderExpression: value => value.HasValue
                        ? new LocationAnchorId(value.Value)
                        : null)
               .HasColumnName("location_anchor_id");

            builder.Property(facility => facility.DailyPatientCapacity)
               .HasColumnName("daily_patient_capacity")
               .IsRequired();

            builder.Property(facility => facility.IsActive)
               .HasColumnName("is_active")
               .IsRequired();

            builder.Property(facility => facility.LastSourceRevision)
               .HasColumnName("last_source_revision")
               .IsRequired();

            builder.Property(facility => facility.LastSynchronizedAtUtc)
               .HasColumnName("last_synchronized_at_utc")
               .IsRequired();

            builder.Property<uint>("xmin")
               .IsRowVersion()
               .HasColumnName("xmin");

            builder.HasIndex(facility => new
                   {
                       facility.SimulationHostId,
                       facility.IsActive,
                       facility.Kind
                   })
               .HasDatabaseName("ix_healthcare_care_facilities_capacity_candidates");
        }
    }
}
