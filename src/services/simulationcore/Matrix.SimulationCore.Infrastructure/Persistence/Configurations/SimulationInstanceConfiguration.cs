using Matrix.Simulation.Primitives;
using Matrix.SimulationCore.Domain.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.SimulationCore.Infrastructure.Persistence.Configurations;

public sealed class SimulationInstanceConfiguration : IEntityTypeConfiguration<SimulationInstance>
{
    public void Configure(EntityTypeBuilder<SimulationInstance> builder)
    {
        builder.ToTable("SimulationInstances");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
           .HasConversion(
                convertToProviderExpression: x => x.Value,
                convertFromProviderExpression: x => new SimulationId(x))
           .ValueGeneratedNever();

        builder.Property(x => x.HostId)
           .HasConversion(
                convertToProviderExpression: x => x.Value,
                convertFromProviderExpression: x => new SimulationHostId(x))
           .IsRequired();

        builder.Property(x => x.ScenarioKey)
           .HasConversion(
                convertToProviderExpression: x => x.Value,
                convertFromProviderExpression: x => new SimulationScenarioKey(x))
           .HasMaxLength(SimulationScenarioKey.MaxLength)
           .IsRequired();

        builder.Property(x => x.HostTypeKey)
           .HasConversion(
                convertToProviderExpression: x => x.Value,
                convertFromProviderExpression: x => new SimulationHostTypeKey(x))
           .HasMaxLength(SimulationHostTypeKey.MaxLength)
           .IsRequired();

        builder.Property(x => x.Seed)
           .HasConversion(
                convertToProviderExpression: x => x.Value,
                convertFromProviderExpression: x => new SimulationSeed(x))
           .HasMaxLength(SimulationSeed.MaxLength)
           .IsRequired();

        builder.Property(x => x.RunId)
           .IsRequired();

        builder.Property(x => x.ModelVersion)
           .HasConversion(
                convertToProviderExpression: x => x.Value,
                convertFromProviderExpression: x => new SimulationModelVersion(x))
           .HasMaxLength(SimulationModelVersion.MaxLength)
           .IsRequired();

        builder.Property(x => x.ProvisioningCorrelationId)
           .IsRequired(false);

        builder.Property(x => x.State)
           .HasConversion<int>()
           .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
           .IsRequired();

        builder.Property(x => x.ArchivedAtUtc)
           .IsRequired(false);

        builder.Ignore(x => x.RuntimeKey);
        builder.Ignore(x => x.IsActive);
        builder.Ignore(x => x.IsArchived);
        builder.Ignore(x => x.DomainEvents);

        builder.HasIndex(x => new
            {
                x.ScenarioKey,
                x.HostTypeKey,
                x.HostId
            })
           .IsUnique();
        builder.HasIndex(x => x.State);
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.ProvisioningCorrelationId)
           .IsUnique()
           .HasFilter("\"ProvisioningCorrelationId\" IS NOT NULL");

        builder.Property<uint>("xmin")
           .HasColumnName("xmin")
           .ValueGeneratedOnAddOrUpdate()
           .IsConcurrencyToken();
    }
}
