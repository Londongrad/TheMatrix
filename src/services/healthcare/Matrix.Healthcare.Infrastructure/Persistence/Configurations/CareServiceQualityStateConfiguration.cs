using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Healthcare.Infrastructure.Persistence.Configurations;

public sealed class CareServiceQualityStateConfiguration
    : IEntityTypeConfiguration<CareServiceQualityState>
{
    public void Configure(EntityTypeBuilder<CareServiceQualityState> builder)
    {
        builder.ToTable(
            "healthcare_care_service_quality_states",
            table => table.HasCheckConstraint(
                "ck_healthcare_care_service_quality_multiplier",
                "quality_multiplier BETWEEN 0 AND 2"));

        builder.HasKey(state => state.Id);

        builder.Property(state => state.Id)
           .HasConversion(
                convertToProviderExpression: id => id.Value,
                convertFromProviderExpression: value => new SimulationHostId(value))
           .HasColumnName("simulation_host_id");

        builder.Property(state => state.QualityMultiplier)
           .HasConversion(
                convertToProviderExpression: multiplier => multiplier.Value,
                convertFromProviderExpression: value => new CareQualityMultiplier(value))
           .HasPrecision(5, 4)
           .HasColumnName("quality_multiplier")
           .IsRequired();

        builder.Property(state => state.LastObservedAtUtc)
           .HasColumnName("last_observed_at_utc")
           .IsRequired();

        builder.Property<uint>("xmin")
           .IsRowVersion()
           .HasColumnName("xmin");
    }
}
