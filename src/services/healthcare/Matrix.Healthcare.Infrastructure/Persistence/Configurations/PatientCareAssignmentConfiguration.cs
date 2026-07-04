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

        builder.HasOne<CareFacility>()
           .WithMany()
           .HasForeignKey(assignment => assignment.CareFacilityId)
           .OnDelete(DeleteBehavior.Restrict);
    }
}
