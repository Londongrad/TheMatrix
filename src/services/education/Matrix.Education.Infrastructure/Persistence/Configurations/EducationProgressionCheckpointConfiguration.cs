using Matrix.Education.Domain.Progression;
using Matrix.Education.Domain.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Education.Infrastructure.Persistence.Configurations
{
    public sealed class EducationProgressionCheckpointConfiguration
        : IEntityTypeConfiguration<EducationProgressionCheckpoint>
    {
        public void Configure(EntityTypeBuilder<EducationProgressionCheckpoint> builder)
        {
            builder.ToTable(
                "education_progression_checkpoints",
                table => table.HasCheckConstraint(
                    "ck_education_progression_checkpoint_tick",
                    "last_completed_tick_id >= 0"));

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => new SimulationHostId(value))
               .HasColumnName("simulation_host_id");

            builder.Property(x => x.LastCompletedTickId)
               .HasColumnName("last_completed_tick_id")
               .IsRequired();

            builder.Property(x => x.LastCompletedAtUtc)
               .HasColumnName("last_completed_at_utc")
               .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
               .HasColumnName("updated_at_utc")
               .IsRequired();
        }
    }
}
