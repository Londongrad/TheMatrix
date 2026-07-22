using Matrix.Education.Domain.Simulation;
using Matrix.Education.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Education.Infrastructure.Persistence.Configurations;

public sealed class EducationSimulationRuntimeStateConfiguration : IEntityTypeConfiguration<EducationSimulationRuntimeState>
{
    public void Configure(EntityTypeBuilder<EducationSimulationRuntimeState> builder)
    {
        builder.ToTable("education_simulation_runtimes");
        builder.HasKey(x => x.SimulationHostId);
        builder.Property(x => x.SimulationHostId)
            .HasConversion(id => id.Value, value => new SimulationHostId(value))
            .HasColumnName("simulation_host_id");
        builder.Property(x => x.ScenarioKey).HasColumnName("scenario_key").IsRequired();
        builder.Property(x => x.HostTypeKey).HasColumnName("host_type_key").IsRequired();
    }
}
