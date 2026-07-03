using Matrix.Healthcare.Domain.Progression;
using Matrix.Healthcare.Domain.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Healthcare.Infrastructure.Persistence.Configurations;

public sealed class PatientHealthProgressionBatchSetConfiguration
    : IEntityTypeConfiguration<PatientHealthProgressionBatchSet>
{
    public void Configure(EntityTypeBuilder<PatientHealthProgressionBatchSet> builder)
    {
        builder.ToTable("healthcare_patient_health_progression_batch_sets");

        builder.HasKey(batchSet => new
        {
            batchSet.SimulationHostId,
            batchSet.SourceRevision
        });

        builder.Property(batchSet => batchSet.SimulationHostId)
           .HasConversion(
                convertToProviderExpression: id => id.Value,
                convertFromProviderExpression: value => new SimulationHostId(value))
           .HasColumnName("simulation_host_id")
           .IsRequired();

        builder.Property(batchSet => batchSet.SourceRevision)
           .HasColumnName("source_revision")
           .IsRequired();

        builder.Property(batchSet => batchSet.CorrelationId)
           .HasColumnName("correlation_id")
           .HasMaxLength(PatientHealthProgressionBatchSet.MaxCorrelationIdLength)
           .IsRequired();

        builder.Property(batchSet => batchSet.TotalBatches)
           .HasColumnName("total_batches")
           .IsRequired();

        builder.Property(batchSet => batchSet.ReceivedBatchCount)
           .HasColumnName("received_batch_count")
           .IsRequired();

        builder.Property(batchSet => batchSet.IsComplete)
           .HasColumnName("is_complete")
           .IsRequired();

        builder.Property(batchSet => batchSet.FirstReceivedAtUtc)
           .HasColumnName("first_received_at_utc")
           .IsRequired();

        builder.Property(batchSet => batchSet.LastReceivedAtUtc)
           .HasColumnName("last_received_at_utc")
           .IsRequired();

        builder.Property(batchSet => batchSet.CompletedAtUtc)
           .HasColumnName("completed_at_utc");

        builder.Property<byte[]>("_receivedBatchMap")
           .HasColumnName("received_batch_map")
           .IsRequired();

        builder.Property<uint>("xmin")
           .IsRowVersion()
           .HasColumnName("xmin");
    }
}
