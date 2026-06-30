using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
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

            builder.Property(x => x.LastSourceRevision)
               .HasColumnName("last_source_revision")
               .IsRequired();

            builder.Property(x => x.LastLifecycleRevision)
               .HasColumnName("last_lifecycle_revision")
               .HasDefaultValue(0L)
               .IsRequired();

            builder.Property(x => x.LastSynchronizedAtUtc)
               .HasColumnName("last_synchronized_at_utc")
               .IsRequired();

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
