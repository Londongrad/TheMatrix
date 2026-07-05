using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Healthcare.Infrastructure.Persistence.Configurations;

public sealed class CareMedicineSupplyStateConfiguration
    : IEntityTypeConfiguration<CareMedicineSupplyState>
{
    public void Configure(EntityTypeBuilder<CareMedicineSupplyState> builder)
    {
        builder.ToTable(
            "healthcare_care_medicine_supply_states",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_healthcare_care_medicine_stock_level",
                    "stock_level_index BETWEEN 0 AND 1");
                table.HasCheckConstraint(
                    "ck_healthcare_care_medicine_shortage_risk",
                    "shortage_risk_index BETWEEN 0 AND 1");
                table.HasCheckConstraint(
                    "ck_healthcare_care_medicine_source_revision",
                    "last_source_revision >= 0");
            });

        builder.HasKey(state => state.Id);

        builder.Property(state => state.Id)
           .HasConversion(
                convertToProviderExpression: id => id.Value,
                convertFromProviderExpression: value => new SimulationHostId(value))
           .HasColumnName("simulation_host_id");

        builder.Property(state => state.StockLevel)
           .HasConversion(
                convertToProviderExpression: index => index.Value,
                convertFromProviderExpression: value => new CareAvailabilityIndex(value))
           .HasPrecision(5, 4)
           .HasColumnName("stock_level_index")
           .IsRequired();

        builder.Property(state => state.ShortageRisk)
           .HasConversion(
                convertToProviderExpression: index => index.Value,
                convertFromProviderExpression: value => new CareAvailabilityIndex(value))
           .HasPrecision(5, 4)
           .HasColumnName("shortage_risk_index")
           .IsRequired();

        builder.Property(state => state.LastSourceRevision)
           .HasColumnName("last_source_revision")
           .IsRequired();

        builder.Property(state => state.LastObservedAtUtc)
           .HasColumnName("last_observed_at_utc")
           .IsRequired();

        builder.Property<uint>("xmin")
           .IsRowVersion()
           .HasColumnName("xmin");
    }
}
