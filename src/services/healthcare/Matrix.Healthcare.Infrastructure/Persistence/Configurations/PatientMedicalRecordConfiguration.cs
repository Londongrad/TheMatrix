using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Healthcare.Infrastructure.Persistence.Configurations
{
    public sealed class PatientMedicalRecordConfiguration
        : IEntityTypeConfiguration<PatientMedicalRecord>
    {
        public void Configure(EntityTypeBuilder<PatientMedicalRecord> builder)
        {
            builder.ToTable("healthcare_patient_medical_records");

            builder.HasKey(record => record.Id);

            builder.Property(record => record.Id)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => new PatientId(value))
               .HasColumnName("patient_id");

            builder.Property(record => record.SimulationHostId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => new SimulationHostId(value))
               .HasColumnName("simulation_host_id")
               .IsRequired();

            builder.Property(record => record.Health)
               .HasConversion(
                    convertToProviderExpression: health => health.Value,
                    convertFromProviderExpression: value => new HealthScore(value))
               .HasColumnName("health_score")
               .IsRequired();

            builder.Property(record => record.LastProgressionRevision)
               .HasColumnName("last_progression_revision")
               .HasDefaultValue(-1L)
               .IsRequired();

            builder.Property(record => record.LastLifecycleRevision)
               .HasColumnName("last_lifecycle_revision")
               .HasDefaultValue(0L)
               .IsRequired();

            builder.OwnsOne(record => record.Illness, illness =>
            {
                illness.Property(state => state.CurrentKind)
                   .HasConversion<int?>()
                   .HasColumnName("illness_kind");

                illness.Property(state => state.CurrentSeverity)
                   .HasConversion<int?>()
                   .HasColumnName("illness_severity");

                illness.Property(state => state.DiagnosedOn)
                   .HasColumnType("date")
                   .HasColumnName("illness_diagnosed_on");

                illness.Property(state => state.LastRecoveredOn)
                   .HasColumnType("date")
                   .HasColumnName("last_illness_recovered_on");
            });

            builder.Navigation(record => record.Illness).IsRequired();

            builder.HasIndex(record => record.SimulationHostId)
               .HasDatabaseName("ix_healthcare_medical_records_simulation_host");
        }
    }
}
