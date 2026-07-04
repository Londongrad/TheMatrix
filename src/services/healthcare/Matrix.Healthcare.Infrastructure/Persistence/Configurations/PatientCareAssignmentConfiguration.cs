using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Healthcare.Infrastructure.Persistence.Configurations;

public sealed class PatientCareAssignmentConfiguration
    : IEntityTypeConfiguration<PatientCareAssignment>
{
    public void Configure(EntityTypeBuilder<PatientCareAssignment> builder)
    {
        builder.ToTable(
            "healthcare_patient_care_assignments",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_healthcare_patient_care_assignments_assessment_revision",
                    "assessment_revision >= 0");
                table.HasCheckConstraint(
                    "ck_healthcare_patient_care_assignments_lifecycle_revision",
                    "lifecycle_revision >= 0");
                table.HasCheckConstraint(
                    "ck_healthcare_patient_care_assignments_status",
                    "status BETWEEN 0 AND 2");
                table.HasCheckConstraint(
                    "ck_healthcare_patient_care_assignments_cancellation_reason",
                    "cancellation_reason IS NULL OR cancellation_reason BETWEEN 0 AND 4");
                table.HasCheckConstraint(
                    "ck_healthcare_patient_care_assignments_closure",
                    "(status = 0 AND closed_on IS NULL AND closed_at_utc IS NULL " +
                    "AND cancellation_reason IS NULL AND treatment_health_delta IS NULL " +
                    "AND treatment_medical_state_changed IS NULL) OR " +
                    "(status = 1 AND closed_on IS NOT NULL AND closed_at_utc IS NOT NULL " +
                    "AND cancellation_reason IS NULL AND treatment_health_delta IS NOT NULL " +
                    "AND treatment_medical_state_changed IS NOT NULL) OR " +
                    "(status = 2 AND closed_on IS NOT NULL AND closed_at_utc IS NOT NULL " +
                    "AND cancellation_reason IS NOT NULL AND treatment_health_delta IS NULL " +
                    "AND treatment_medical_state_changed IS NULL)");
            });

        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.Id)
           .HasConversion(
                convertToProviderExpression: id => id.Value,
                convertFromProviderExpression: value => new PatientCareAssignmentId(value))
           .HasColumnName("patient_care_assignment_id");

        builder.Property(assignment => assignment.SimulationHostId)
           .HasConversion(
                convertToProviderExpression: id => id.Value,
                convertFromProviderExpression: value => new SimulationHostId(value))
           .HasColumnName("simulation_host_id")
           .IsRequired();

        builder.Property(assignment => assignment.PatientId)
           .HasConversion(
                convertToProviderExpression: id => id.Value,
                convertFromProviderExpression: value => new PatientId(value))
           .HasColumnName("patient_id")
           .IsRequired();

        builder.Property(assignment => assignment.CareFacilityId)
           .HasConversion(
                convertToProviderExpression: id => id.Value,
                convertFromProviderExpression: value => new CareFacilityId(value))
           .HasColumnName("care_facility_id")
           .IsRequired();

        builder.Property(assignment => assignment.CareDate)
           .HasColumnType("date")
           .HasColumnName("care_date")
           .IsRequired();

        builder.Property(assignment => assignment.Urgency)
           .HasConversion<int>()
           .HasColumnName("urgency")
           .IsRequired();

        builder.Property(assignment => assignment.AssessmentRevision)
           .HasColumnName("assessment_revision")
           .IsRequired();

        builder.Property(assignment => assignment.LifecycleRevision)
           .HasColumnName("lifecycle_revision")
           .IsRequired();

        builder.Property(assignment => assignment.AssignedAtUtc)
           .HasColumnName("assigned_at_utc")
           .IsRequired();

        builder.Property(assignment => assignment.Status)
           .HasConversion<int>()
           .HasColumnName("status")
           .HasDefaultValue(PatientCareAssignmentStatus.Scheduled)
           .IsRequired();

        builder.Property(assignment => assignment.ClosedOn)
           .HasColumnType("date")
           .HasColumnName("closed_on");

        builder.Property(assignment => assignment.ClosedAtUtc)
           .HasColumnName("closed_at_utc");

        builder.Property(assignment => assignment.CancellationReason)
           .HasConversion<int?>()
           .HasColumnName("cancellation_reason");

        builder.Property(assignment => assignment.TreatmentHealthDelta)
           .HasColumnName("treatment_health_delta");

        builder.Property(assignment => assignment.TreatmentMedicalStateChanged)
           .HasColumnName("treatment_medical_state_changed");

        builder.HasIndex(assignment => new
               {
                   assignment.SimulationHostId,
                   assignment.PatientId,
                   assignment.CareDate
               })
           .IsUnique()
           .HasDatabaseName("ux_healthcare_patient_care_assignments_patient_date");

        builder.HasIndex(assignment => new
               {
                   assignment.SimulationHostId,
                   assignment.CareDate,
                   assignment.CareFacilityId
               })
           .HasDatabaseName("ix_healthcare_patient_care_assignments_capacity_usage");

        builder.HasIndex(assignment => new
               {
                   assignment.SimulationHostId,
                   assignment.Status,
                   assignment.PatientId,
                   assignment.CareDate
               })
           .HasDatabaseName("ix_healthcare_patient_care_assignments_due_lookup");

        builder.HasOne<CareFacility>()
           .WithMany()
           .HasForeignKey(assignment => assignment.CareFacilityId)
           .OnDelete(DeleteBehavior.Restrict);
    }
}
