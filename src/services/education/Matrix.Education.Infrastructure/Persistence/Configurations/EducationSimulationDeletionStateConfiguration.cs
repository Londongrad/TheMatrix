using Matrix.Education.Domain.Simulation;
using Matrix.Education.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Education.Infrastructure.Persistence.Configurations
{
    public sealed class EducationSimulationDeletionStateConfiguration
        : IEntityTypeConfiguration<EducationSimulationDeletionState>
    {
        public void Configure(EntityTypeBuilder<EducationSimulationDeletionState> builder)
        {
            builder.ToTable("education_simulation_deletions");
            builder.HasKey(state => state.SimulationHostId);

            builder.Property(state => state.SimulationHostId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => new SimulationHostId(value))
               .HasColumnName("simulation_host_id");

            builder.Property(state => state.DeletedAtUtc)
               .HasColumnName("deleted_at_utc")
               .IsRequired();

            builder.Property(state => state.UpdatedAtUtc)
               .HasColumnName("updated_at_utc")
               .IsRequired();
        }
    }
}
