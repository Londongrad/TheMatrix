using Matrix.Healthcare.Domain.Simulation;
using Matrix.Healthcare.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Healthcare.Infrastructure.Persistence.Configurations
{
    public sealed class HealthcareSimulationDeletionStateConfiguration
        : IEntityTypeConfiguration<HealthcareSimulationDeletionState>
    {
        public void Configure(EntityTypeBuilder<HealthcareSimulationDeletionState> builder)
        {
            builder.ToTable("healthcare_simulation_deletions");
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
