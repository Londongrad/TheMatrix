using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;
using Matrix.Resources.Domain.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Resources.Infrastructure.Persistence.Configurations
{
    public sealed class CityStockpileStateConfiguration : IEntityTypeConfiguration<CityStockpileState>
    {
        public void Configure(EntityTypeBuilder<CityStockpileState> builder)
        {
            builder.ToTable("CityStockpiles");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new SimulationHostId(x))
               .ValueGeneratedNever();

            builder.Property(x => x.SupplyStressIndex)
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .IsRequired();

            builder.Property(x => x.EmergencyRationingEnabled)
               .IsRequired();

            builder.Property(x => x.LastEvaluatedAtUtc)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.OperationalBudgetPressure,
                buildAction: budget =>
                {
                    budget.Property(x => x.Balance)
                       .HasPrecision(
                            precision: 18,
                            scale: 2)
                       .HasColumnName("BudgetPressureBalance")
                       .IsRequired();

                    budget.Property(x => x.MunicipalOperationsExpenses)
                       .HasPrecision(
                            precision: 18,
                            scale: 2)
                       .HasColumnName("BudgetPressureMunicipalOperationsExpenses")
                       .IsRequired();

                    budget.Property(x => x.PressureIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("BudgetPressureIndex")
                       .IsRequired();

                    budget.Property(x => x.EffectiveAtUtc)
                       .HasColumnName("BudgetPressureEffectiveAtUtc")
                       .IsRequired();
                });
            builder.Navigation(x => x.OperationalBudgetPressure)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.SystemsDemand,
                buildAction: demand =>
                {
                    demand.Property(x => x.FuelDemandPressureIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("SystemsDemandFuelDemandPressureIndex")
                       .IsRequired();

                    demand.Property(x => x.SparePartsDemandPressureIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("SystemsDemandSparePartsDemandPressureIndex")
                       .IsRequired();

                    demand.Property(x => x.FiltersDemandPressureIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("SystemsDemandFiltersDemandPressureIndex")
                       .IsRequired();

                    demand.Property(x => x.EmergencyWaterDemandPressureIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("SystemsDemandEmergencyWaterDemandPressureIndex")
                       .IsRequired();

                    demand.Property(x => x.OverallDemandPressureIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("SystemsDemandOverallDemandPressureIndex")
                       .IsRequired();

                    demand.Property(x => x.EffectiveAtUtc)
                       .HasColumnName("SystemsDemandEffectiveAtUtc")
                       .IsRequired();
                });
            builder.Navigation(x => x.SystemsDemand)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.Fuel,
                buildAction: stock => ConfigureLine(stock, "Fuel"));
            builder.Navigation(x => x.Fuel)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.Food,
                buildAction: stock => ConfigureLine(stock, "Food"));
            builder.Navigation(x => x.Food)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.Medicine,
                buildAction: stock => ConfigureLine(stock, "Medicine"));
            builder.Navigation(x => x.Medicine)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.SpareParts,
                buildAction: stock => ConfigureLine(stock, "SpareParts"));
            builder.Navigation(x => x.SpareParts)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.Filters,
                buildAction: stock => ConfigureLine(stock, "Filters"));
            builder.Navigation(x => x.Filters)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.EmergencyWater,
                buildAction: stock => ConfigureLine(stock, "EmergencyWater"));
            builder.Navigation(x => x.EmergencyWater)
               .IsRequired();
        }

        private static void ConfigureLine(
            OwnedNavigationBuilder<CityStockpileState, CityResourceStockLineState> builder,
            string prefix)
        {
            builder.Property(x => x.Kind)
               .HasConversion<string>()
               .HasColumnName($"{prefix}Kind")
               .IsRequired();

            builder.Property(x => x.StockLevelIndex)
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .HasColumnName($"{prefix}StockLevelIndex")
               .IsRequired();

            builder.Property(x => x.DemandPressureIndex)
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .HasColumnName($"{prefix}DemandPressureIndex")
               .IsRequired();

            builder.Property(x => x.ResupplyReadinessIndex)
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .HasColumnName($"{prefix}ResupplyReadinessIndex")
               .IsRequired();

            builder.Property(x => x.ShortageRiskIndex)
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .HasColumnName($"{prefix}ShortageRiskIndex")
               .IsRequired();
        }
    }
}
