using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Healthcare.Infrastructure.Persistence.Configurations;

public sealed class PatientCareNeedConfiguration : IEntityTypeConfiguration<PatientCareNeed>
{
    public void Configure(EntityTypeBuilder<PatientCareNeed> builder)
    {
        builder.ToTable("healthcare_patient_care_needs");

        builder.HasKey(careNeed => careNeed.Id);

        builder.Property(careNeed => careNeed.Id)
           .HasConversion(
                convertToProviderExpression: id => id.Value,
                convertFromProviderExpression: value => new PatientId(value))
           .HasColumnName("patient_id");

        builder.Property(careNeed => careNeed.SimulationHostId)
           .HasConversion(
                convertToProviderExpression: id => id.Value,
                convertFromProviderExpression: value => new SimulationHostId(value))
           .HasColumnName("simulation_host_id")
           .IsRequired();

        builder.Property(careNeed => careNeed.Urgency)
           .HasConversion<int>()
           .HasColumnName("urgency")
           .IsRequired();

        builder.Property(careNeed => careNeed.IsActive)
           .HasColumnName("is_active")
           .IsRequired();

        builder.Property(careNeed => careNeed.RequestedOn)
           .HasColumnType("date")
           .HasColumnName("requested_on")
           .IsRequired();

        builder.Property(careNeed => careNeed.ResolvedOn)
           .HasColumnType("date")
           .HasColumnName("resolved_on");

        builder.Property(careNeed => careNeed.LastAssessmentRevision)
           .HasColumnName("last_assessment_revision")
           .IsRequired();

        builder.Property(careNeed => careNeed.LastLifecycleRevision)
           .HasColumnName("last_lifecycle_revision")
           .IsRequired();

        builder.Property(careNeed => careNeed.LastAssessedAtUtc)
           .HasColumnName("last_assessed_at_utc")
           .IsRequired();

        builder.Property<uint>("xmin")
           .IsRowVersion()
           .HasColumnName("xmin");

        builder.HasIndex(careNeed => new
               {
                   careNeed.SimulationHostId,
                   careNeed.IsActive,
                   careNeed.Urgency,
                   careNeed.RequestedOn
               })
           .HasDatabaseName("ix_healthcare_patient_care_needs_allocation_candidates");
    }
}
