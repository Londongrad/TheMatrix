using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Matrix.Education.Domain.Programs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Education.Infrastructure.Persistence.Configurations
{
    public sealed class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
    {
        public void Configure(EntityTypeBuilder<StudentProfile> builder)
        {
            builder.ToTable("education_student_profiles");

            builder.HasKey(x => x.Id);
            builder.HasAlternateKey(x => new
            {
                x.SimulationHostId,
                x.Id
            });

            builder.Property(x => x.Id)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => new ResidentId(value))
               .HasColumnName("resident_id");

            builder.Property(x => x.SimulationHostId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => new SimulationHostId(value))
               .HasColumnName("simulation_host_id")
               .IsRequired();

            builder.Property(x => x.BirthDate)
               .HasConversion(
                    convertToProviderExpression: value => value.ToDateTime(TimeOnly.MinValue),
                    convertFromProviderExpression: value => DateOnly.FromDateTime(value))
               .HasColumnType("date")
               .HasColumnName("birth_date")
               .IsRequired();

            builder.Property(x => x.IsAlive)
               .HasColumnName("is_alive")
               .IsRequired();

            builder.Property(x => x.IsActive)
               .HasColumnName("is_active")
               .IsRequired();

            builder.Property(x => x.CompletedStage)
               .HasConversion(
                    convertToProviderExpression: stage => stage.HasValue
                        ? stage.Value.Value
                        : null,
                    convertFromProviderExpression: value => value == null
                        ? null
                        : new EducationStageKey(value))
               .HasMaxLength(64)
               .HasColumnName("completed_stage");

            builder.Property(x => x.CompletedStageOn)
               .HasConversion(
                    convertToProviderExpression: value => value.HasValue
                        ? value.Value.ToDateTime(TimeOnly.MinValue)
                        : (DateTime?)null,
                    convertFromProviderExpression: value => value.HasValue
                        ? DateOnly.FromDateTime(value.Value)
                        : null)
               .HasColumnType("date")
               .HasColumnName("completed_stage_on");

            builder.Property(x => x.LastSourceRevision)
               .HasColumnName("last_source_revision")
               .IsRequired();

            builder.Property(x => x.LastLifecycleRevision)
               .HasColumnName("last_lifecycle_revision")
               .HasDefaultValue(0L)
               .IsRequired();

            builder.Property(x => x.ParticipationRevision)
               .HasColumnName("participation_revision")
               .HasDefaultValue(0L)
               .IsRequired();

            builder.Property(x => x.LastSynchronizedAtUtc)
               .HasColumnName("last_synchronized_at_utc")
               .IsRequired();

            builder.Property(x => x.LastAttendanceSourceTickId).HasColumnName("last_attendance_source_tick_id");
            builder.Property(x => x.AttendanceObservedAtSimTimeUtc).HasColumnName("attendance_observed_at_sim_time_utc");
            builder.Property(x => x.AttendanceIndex).HasColumnName("attendance_index");
            builder.Property(x => x.CommuteAccessibilityIndex).HasColumnName("commute_accessibility_index");

            builder.HasIndex(x => new
                   {
                       x.SimulationHostId,
                       x.IsActive,
                       x.IsAlive,
                       x.BirthDate
                   })
               .HasDatabaseName("ix_education_profiles_tick_candidates");
        }
    }
}
