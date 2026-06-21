using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Programs;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Education.Infrastructure.Persistence.Configurations
{
    public sealed class StudentEnrollmentConfiguration
        : IEntityTypeConfiguration<StudentEnrollment>
    {
        public void Configure(EntityTypeBuilder<StudentEnrollment> builder)
        {
            builder.ToTable(
                "education_enrollments",
                table => table.HasCheckConstraint(
                    "ck_education_enrollments_terminal_date",
                    "(status = 'Active' AND closed_on IS NULL) OR " +
                    "(status <> 'Active' AND closed_on IS NOT NULL)"));

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => new EnrollmentId(value))
               .HasColumnName("enrollment_id");

            builder.Property(x => x.SimulationHostId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => new SimulationHostId(value))
               .HasColumnName("simulation_host_id")
               .IsRequired();

            builder.Property(x => x.ResidentId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => new ResidentId(value))
               .HasColumnName("resident_id")
               .IsRequired();

            builder.Property(x => x.InstitutionId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => new EducationInstitutionId(value))
               .HasColumnName("institution_id")
               .IsRequired();

            builder.Property(x => x.Stage)
               .HasConversion(
                    convertToProviderExpression: key => key.Value,
                    convertFromProviderExpression: value => new EducationStageKey(value))
               .HasMaxLength(64)
               .HasColumnName("stage")
               .IsRequired();

            builder.Property(x => x.EnrolledOn)
               .HasConversion(
                    convertToProviderExpression: value => value.ToDateTime(TimeOnly.MinValue),
                    convertFromProviderExpression: value => DateOnly.FromDateTime(value))
               .HasColumnType("date")
               .HasColumnName("enrolled_on")
               .IsRequired();

            builder.Property(x => x.Status)
               .HasConversion<string>()
               .HasMaxLength(16)
               .HasColumnName("status")
               .IsRequired();

            builder.Property(x => x.ClosedOn)
               .HasConversion(
                    convertToProviderExpression: value => value.HasValue
                        ? value.Value.ToDateTime(TimeOnly.MinValue)
                        : (DateTime?)null,
                    convertFromProviderExpression: value => value.HasValue
                        ? DateOnly.FromDateTime(value.Value)
                        : null)
               .HasColumnType("date")
               .HasColumnName("closed_on");

            builder.HasOne<StudentProfile>()
               .WithMany()
               .HasForeignKey(x => x.ResidentId)
               .HasPrincipalKey(x => x.Id)
               .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<EducationInstitution>()
               .WithMany()
               .HasForeignKey(x => x.InstitutionId)
               .HasPrincipalKey(x => x.Id)
               .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new
                   {
                       x.SimulationHostId,
                       x.Status,
                       x.Stage,
                       x.ResidentId
                   })
               .HasDatabaseName("ix_education_enrollments_tick_candidates");

            builder.HasIndex(x => new
                   {
                       x.InstitutionId,
                       x.Status
                   })
               .HasDatabaseName("ix_education_enrollments_institution_status");

            builder.HasIndex(x => new
                   {
                       x.SimulationHostId,
                       x.ResidentId,
                       x.Stage
                   })
               .IsUnique()
               .HasFilter("status = 'Active'")
               .HasDatabaseName("ux_education_enrollments_active_stage");
        }
    }
}
