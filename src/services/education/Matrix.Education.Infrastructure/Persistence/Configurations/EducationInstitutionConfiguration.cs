using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Education.Infrastructure.Persistence.Configurations
{
    public sealed class EducationInstitutionConfiguration
        : IEntityTypeConfiguration<EducationInstitution>
    {
        public void Configure(EntityTypeBuilder<EducationInstitution> builder)
        {
            builder.ToTable(
                "education_institutions",
                table =>
                {
                    table.HasCheckConstraint(
                        "ck_education_institutions_capacity_positive",
                        "capacity > 0");
                    table.HasCheckConstraint(
                        "ck_education_institutions_enrollment_within_capacity",
                        "current_enrollment_count >= 0 AND current_enrollment_count <= capacity");
                });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => new EducationInstitutionId(value))
               .HasColumnName("institution_id");

            builder.Property(x => x.SimulationHostId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => new SimulationHostId(value))
               .HasColumnName("simulation_host_id")
               .IsRequired();

            builder.Property(x => x.Name)
               .HasMaxLength(EducationInstitution.MaxNameLength)
               .HasColumnName("name")
               .IsRequired();

            builder.Property(x => x.Kind)
               .HasConversion(
                    convertToProviderExpression: key => key.Value,
                    convertFromProviderExpression: value => new EducationInstitutionKindKey(value))
               .HasMaxLength(64)
               .HasColumnName("kind")
               .IsRequired();

            builder.Property(x => x.LocationAnchorId)
               .HasConversion(
                    convertToProviderExpression: id => id.HasValue
                        ? id.Value.Value
                        : (Guid?)null,
                    convertFromProviderExpression: value => value.HasValue
                        ? new LocationAnchorId(value.Value)
                        : null)
               .HasColumnName("location_anchor_id");

            builder.Property(x => x.Capacity)
               .HasColumnName("capacity")
               .IsRequired();

            builder.Property(x => x.CurrentEnrollmentCount)
               .HasColumnName("current_enrollment_count")
               .IsRequired();

            builder.Property(x => x.IsActive)
               .HasColumnName("is_active")
               .IsRequired();

            builder.Property<uint>("xmin")
               .IsRowVersion()
               .HasColumnName("xmin");

            builder.HasIndex(x => new
                   {
                       x.SimulationHostId,
                       x.IsActive,
                       x.Kind
                   })
               .HasDatabaseName("ix_education_institutions_capacity_candidates");
        }
    }
}
