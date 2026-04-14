using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.SimulationSystems.Domain.Simulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.SimulationSystems.Infrastructure.Persistence.Configurations
{
    public sealed class CityEnvironmentalConditionStateConfiguration
        : IEntityTypeConfiguration<CityEnvironmentalConditionState>
    {
        public void Configure(EntityTypeBuilder<CityEnvironmentalConditionState> builder)
        {
            builder.ToTable("CityEnvironmentalConditions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new SimulationHostId(x))
               .ValueGeneratedNever();

            builder.Property(x => x.LastEvaluatedAtUtc)
               .IsRequired();

            builder.Property(x => x.LastAppliedTickId)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.WeatherPressure,
                buildAction: pressure =>
                {
                    pressure.Property(x => x.RainPressure)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("WeatherRainPressure")
                       .IsRequired();

                    pressure.Property(x => x.SnowPressure)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("WeatherSnowPressure")
                       .IsRequired();

                    pressure.Property(x => x.StormPressure)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("WeatherStormPressure")
                       .IsRequired();

                    pressure.Property(x => x.FreezePressure)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("WeatherFreezePressure")
                       .IsRequired();

                    pressure.Property(x => x.ThawRelief)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("WeatherThawRelief")
                       .IsRequired();
                });

            builder.Navigation(x => x.WeatherPressure)
               .IsRequired();

            builder.Property(x => x.FloodingIndex)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => FloodingIndex.From(x))
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .IsRequired();

            builder.Property(x => x.SnowAccumulationIndex)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => SnowAccumulationIndex.From(x))
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .IsRequired();

            builder.Property(x => x.RoadAccessibilityIndex)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => RoadAccessibilityIndex.From(x))
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .IsRequired();

            builder.Property(x => x.HeatingCoverageIndex)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => HeatingCoverageIndex.From(x))
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .IsRequired();

            builder.Property(x => x.WaterCoverageIndex)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => WaterCoverageIndex.From(x))
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .IsRequired();

            builder.Property(x => x.SanitationCoverageIndex)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => SanitationCoverageIndex.From(x))
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .IsRequired();

            builder.Property(x => x.PowerCoverageIndex)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => PowerCoverageIndex.From(x))
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .IsRequired();

            builder.Property(x => x.UtilityContinuityIndex)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => UtilityContinuityIndex.From(x))
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.Drainage,
                buildAction: state => ConfigureSystemState(
                    builder: state,
                    prefix: "Drainage"));

            builder.Navigation(x => x.Drainage)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.DrainageInfrastructure,
                buildAction: drainage =>
                {
                    drainage.Property(x => x.PumpCapacityIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("DrainagePumpCapacityIndex")
                       .IsRequired();

                    drainage.Property(x => x.NetworkIntegrityIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("DrainageNetworkIntegrityIndex")
                       .IsRequired();

                    drainage.Property(x => x.BlockageIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("DrainageBlockageIndex")
                       .IsRequired();

                    drainage.Property(x => x.CrewReadinessIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("DrainageCrewReadinessIndex")
                       .IsRequired();

                    drainage.Property(x => x.IncidentPressureIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("DrainageIncidentPressureIndex")
                       .IsRequired();

                    drainage.Property(x => x.EmergencyModeEnabled)
                       .HasColumnName("DrainageEmergencyModeEnabled")
                       .IsRequired();
                });

            builder.Navigation(x => x.DrainageInfrastructure)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.PendingDrainageMaintenance,
                buildAction: pending => ConfigurePendingOperationalWork(
                    builder: pending,
                    prefix: "PendingDrainageMaintenance"));
            builder.Navigation(x => x.PendingDrainageMaintenance)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.SnowRemoval,
                buildAction: state => ConfigureSystemState(
                    builder: state,
                    prefix: "SnowRemoval"));

            builder.Navigation(x => x.SnowRemoval)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.SnowRemovalInfrastructure,
                buildAction: snowRemoval =>
                {
                    snowRemoval.Property(x => x.FleetAvailabilityIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("SnowRemovalFleetAvailabilityIndex")
                       .IsRequired();

                    snowRemoval.Property(x => x.RouteCoverageIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("SnowRemovalRouteCoverageIndex")
                       .IsRequired();

                    snowRemoval.Property(x => x.DeicingReadinessIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("SnowRemovalDeicingReadinessIndex")
                       .IsRequired();

                    snowRemoval.Property(x => x.CrewReadinessIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("SnowRemovalCrewReadinessIndex")
                       .IsRequired();

                    snowRemoval.Property(x => x.IncidentPressureIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("SnowRemovalIncidentPressureIndex")
                       .IsRequired();

                    snowRemoval.Property(x => x.EmergencyModeEnabled)
                       .HasColumnName("SnowRemovalEmergencyModeEnabled")
                       .IsRequired();
                });

            builder.Navigation(x => x.SnowRemovalInfrastructure)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.PendingSnowRemovalMaintenance,
                buildAction: pending => ConfigurePendingOperationalWork(
                    builder: pending,
                    prefix: "PendingSnowRemovalMaintenance"));
            builder.Navigation(x => x.PendingSnowRemovalMaintenance)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.RoadAccess,
                buildAction: state => ConfigureSystemState(
                    builder: state,
                    prefix: "RoadAccess"));

            builder.Navigation(x => x.RoadAccess)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.RoadAccessInfrastructure,
                buildAction: roadAccess =>
                {
                    roadAccess.Property(x => x.CorridorAvailabilityIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("RoadAccessCorridorAvailabilityIndex")
                       .IsRequired();

                    roadAccess.Property(x => x.SurfaceIntegrityIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("RoadAccessSurfaceIntegrityIndex")
                       .IsRequired();

                    roadAccess.Property(x => x.TrafficControlReadinessIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("RoadAccessTrafficControlReadinessIndex")
                       .IsRequired();

                    roadAccess.Property(x => x.CrewReadinessIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("RoadAccessCrewReadinessIndex")
                       .IsRequired();

                    roadAccess.Property(x => x.IncidentPressureIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("RoadAccessIncidentPressureIndex")
                       .IsRequired();

                    roadAccess.Property(x => x.EmergencyModeEnabled)
                       .HasColumnName("RoadAccessEmergencyModeEnabled")
                       .IsRequired();
                });

            builder.Navigation(x => x.RoadAccessInfrastructure)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.PendingRoadAccessMaintenance,
                buildAction: pending => ConfigurePendingOperationalWork(
                    builder: pending,
                    prefix: "PendingRoadAccessMaintenance"));
            builder.Navigation(x => x.PendingRoadAccessMaintenance)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.Heating,
                buildAction: state => ConfigureSystemState(
                    builder: state,
                    prefix: "Heating"));

            builder.Navigation(x => x.Heating)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.HeatingInfrastructure,
                buildAction: heating =>
                {
                    heating.Property(x => x.PlantCapacityIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("HeatingPlantCapacityIndex")
                       .IsRequired();

                    heating.Property(x => x.NetworkIntegrityIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("HeatingNetworkIntegrityIndex")
                       .IsRequired();

                    heating.Property(x => x.ControlReadinessIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("HeatingControlReadinessIndex")
                       .IsRequired();

                    heating.Property(x => x.CrewReadinessIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("HeatingCrewReadinessIndex")
                       .IsRequired();

                    heating.Property(x => x.IncidentPressureIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("HeatingIncidentPressureIndex")
                       .IsRequired();

                    heating.Property(x => x.EmergencyModeEnabled)
                       .HasColumnName("HeatingEmergencyModeEnabled")
                       .IsRequired();
                });

            builder.Navigation(x => x.HeatingInfrastructure)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.PendingHeatingMaintenance,
                buildAction: pending => ConfigurePendingOperationalWork(
                    builder: pending,
                    prefix: "PendingHeatingMaintenance"));
            builder.Navigation(x => x.PendingHeatingMaintenance)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.WaterDistribution,
                buildAction: state => ConfigureSystemState(
                    builder: state,
                    prefix: "WaterDistribution"));

            builder.Navigation(x => x.WaterDistribution)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.WaterDistributionInfrastructure,
                buildAction: waterDistribution =>
                {
                    waterDistribution.Property(x => x.TreatmentCapacityIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("WaterDistributionTreatmentCapacityIndex")
                       .IsRequired();

                    waterDistribution.Property(x => x.NetworkIntegrityIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("WaterDistributionNetworkIntegrityIndex")
                       .IsRequired();

                    waterDistribution.Property(x => x.PumpReadinessIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("WaterDistributionPumpReadinessIndex")
                       .IsRequired();

                    waterDistribution.Property(x => x.CrewReadinessIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("WaterDistributionCrewReadinessIndex")
                       .IsRequired();

                    waterDistribution.Property(x => x.IncidentPressureIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("WaterDistributionIncidentPressureIndex")
                       .IsRequired();

                    waterDistribution.Property(x => x.EmergencyModeEnabled)
                       .HasColumnName("WaterDistributionEmergencyModeEnabled")
                       .IsRequired();
                });

            builder.Navigation(x => x.WaterDistributionInfrastructure)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.PendingWaterDistributionMaintenance,
                buildAction: pending => ConfigurePendingOperationalWork(
                    builder: pending,
                    prefix: "PendingWaterDistributionMaintenance"));
            builder.Navigation(x => x.PendingWaterDistributionMaintenance)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.Sanitation,
                buildAction: state => ConfigureSystemState(
                    builder: state,
                    prefix: "Sanitation"));

            builder.Navigation(x => x.Sanitation)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.SanitationInfrastructure,
                buildAction: sanitation =>
                {
                    sanitation.Property(x => x.TreatmentStabilityIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("SanitationTreatmentStabilityIndex")
                       .IsRequired();

                    sanitation.Property(x => x.NetworkIntegrityIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("SanitationNetworkIntegrityIndex")
                       .IsRequired();

                    sanitation.Property(x => x.OverflowControlIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("SanitationOverflowControlIndex")
                       .IsRequired();

                    sanitation.Property(x => x.CrewReadinessIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("SanitationCrewReadinessIndex")
                       .IsRequired();

                    sanitation.Property(x => x.IncidentPressureIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("SanitationIncidentPressureIndex")
                       .IsRequired();

                    sanitation.Property(x => x.EmergencyModeEnabled)
                       .HasColumnName("SanitationEmergencyModeEnabled")
                       .IsRequired();
                });

            builder.Navigation(x => x.SanitationInfrastructure)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.PendingSanitationMaintenance,
                buildAction: pending => ConfigurePendingOperationalWork(
                    builder: pending,
                    prefix: "PendingSanitationMaintenance"));
            builder.Navigation(x => x.PendingSanitationMaintenance)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.PowerDistribution,
                buildAction: state => ConfigureSystemState(
                    builder: state,
                    prefix: "PowerDistribution"));

            builder.Navigation(x => x.PowerDistribution)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.PowerDistributionInfrastructure,
                buildAction: powerDistribution =>
                {
                    powerDistribution.Property(x => x.SubstationCapacityIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("PowerDistributionSubstationCapacityIndex")
                       .IsRequired();

                    powerDistribution.Property(x => x.GridIntegrityIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("PowerDistributionGridIntegrityIndex")
                       .IsRequired();

                    powerDistribution.Property(x => x.SwitchingReadinessIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("PowerDistributionSwitchingReadinessIndex")
                       .IsRequired();

                    powerDistribution.Property(x => x.CrewReadinessIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("PowerDistributionCrewReadinessIndex")
                       .IsRequired();

                    powerDistribution.Property(x => x.IncidentPressureIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("PowerDistributionIncidentPressureIndex")
                       .IsRequired();

                    powerDistribution.Property(x => x.EmergencyModeEnabled)
                       .HasColumnName("PowerDistributionEmergencyModeEnabled")
                       .IsRequired();
                });

            builder.Navigation(x => x.PowerDistributionInfrastructure)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.PendingPowerDistributionMaintenance,
                buildAction: pending => ConfigurePendingOperationalWork(
                    builder: pending,
                    prefix: "PendingPowerDistributionMaintenance"));
            builder.Navigation(x => x.PendingPowerDistributionMaintenance)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.UtilityIncidents,
                buildAction: state => ConfigureSystemState(
                    builder: state,
                    prefix: "UtilityIncidents"));

            builder.Navigation(x => x.UtilityIncidents)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.UtilityIncidentInfrastructure,
                buildAction: utilityIncidents =>
                {
                    utilityIncidents.Property(x => x.DispatchReadinessIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("UtilityIncidentsDispatchReadinessIndex")
                       .IsRequired();

                    utilityIncidents.Property(x => x.RestorationCoverageIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("UtilityIncidentsRestorationCoverageIndex")
                       .IsRequired();

                    utilityIncidents.Property(x => x.SpareCapacityIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("UtilityIncidentsSpareCapacityIndex")
                       .IsRequired();

                    utilityIncidents.Property(x => x.FieldCoordinationIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("UtilityIncidentsFieldCoordinationIndex")
                       .IsRequired();

                    utilityIncidents.Property(x => x.IncidentQueuePressureIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("UtilityIncidentsIncidentQueuePressureIndex")
                       .IsRequired();

                    utilityIncidents.Property(x => x.EmergencyModeEnabled)
                       .HasColumnName("UtilityIncidentsEmergencyModeEnabled")
                       .IsRequired();
                });

            builder.Navigation(x => x.UtilityIncidentInfrastructure)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.PendingUtilityIncidentResponse,
                buildAction: pending => ConfigurePendingOperationalWork(
                    builder: pending,
                    prefix: "PendingUtilityIncidentResponse"));
            builder.Navigation(x => x.PendingUtilityIncidentResponse)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.ResourceSupply,
                buildAction: resourceSupply =>
                {
                    resourceSupply.Property(x => x.SupplyStressIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("ResourceSupplyStressIndex")
                       .IsRequired();

                    resourceSupply.Property(x => x.FuelStockLevelIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("ResourceFuelStockLevelIndex")
                       .IsRequired();

                    resourceSupply.Property(x => x.FuelResupplyReadinessIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("ResourceFuelResupplyReadinessIndex")
                       .IsRequired();

                    resourceSupply.Property(x => x.FuelShortageRiskIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("ResourceFuelShortageRiskIndex")
                       .IsRequired();

                    resourceSupply.Property(x => x.SparePartsStockLevelIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("ResourceSparePartsStockLevelIndex")
                       .IsRequired();

                    resourceSupply.Property(x => x.SparePartsResupplyReadinessIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("ResourceSparePartsResupplyReadinessIndex")
                       .IsRequired();

                    resourceSupply.Property(x => x.SparePartsShortageRiskIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("ResourceSparePartsShortageRiskIndex")
                       .IsRequired();

                    resourceSupply.Property(x => x.FiltersStockLevelIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("ResourceFiltersStockLevelIndex")
                       .IsRequired();

                    resourceSupply.Property(x => x.FiltersResupplyReadinessIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("ResourceFiltersResupplyReadinessIndex")
                       .IsRequired();

                    resourceSupply.Property(x => x.FiltersShortageRiskIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("ResourceFiltersShortageRiskIndex")
                       .IsRequired();

                    resourceSupply.Property(x => x.EmergencyWaterStockLevelIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("ResourceEmergencyWaterStockLevelIndex")
                       .IsRequired();

                    resourceSupply.Property(x => x.EmergencyWaterResupplyReadinessIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("ResourceEmergencyWaterResupplyReadinessIndex")
                       .IsRequired();

                    resourceSupply.Property(x => x.EmergencyWaterShortageRiskIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("ResourceEmergencyWaterShortageRiskIndex")
                       .IsRequired();

                    resourceSupply.Property(x => x.EffectiveTickId)
                       .HasColumnName("ResourceEffectiveTickId")
                       .IsRequired();

                    resourceSupply.Property(x => x.EffectiveAtUtc)
                       .HasColumnName("ResourceEffectiveAtUtc")
                       .IsRequired();
                });

            builder.Navigation(x => x.ResourceSupply)
               .IsRequired();

            builder.OwnsOne(
                navigationExpression: x => x.OperationalBudgetPressure,
                buildAction: budgetPressure =>
                {
                    budgetPressure.Property(x => x.Balance)
                       .HasPrecision(
                            precision: 18,
                            scale: 2)
                       .HasColumnName("BudgetPressureBalance")
                       .IsRequired();

                    budgetPressure.Property(x => x.MunicipalOperationsExpenses)
                       .HasPrecision(
                            precision: 18,
                            scale: 2)
                       .HasColumnName("BudgetPressureMunicipalOperationsExpenses")
                       .IsRequired();

                    budgetPressure.Property(x => x.GeneralAvailableAmount)
                       .HasPrecision(
                            precision: 18,
                            scale: 2)
                       .HasColumnName("BudgetPressureGeneralAvailableAmount")
                       .IsRequired();

                    budgetPressure.Property(x => x.OperationsAvailableAmount)
                       .HasPrecision(
                            precision: 18,
                            scale: 2)
                       .HasColumnName("BudgetPressureOperationsAvailableAmount")
                       .IsRequired();

                    budgetPressure.Property(x => x.InfrastructureAvailableAmount)
                       .HasPrecision(
                            precision: 18,
                            scale: 2)
                       .HasColumnName("BudgetPressureInfrastructureAvailableAmount")
                       .IsRequired();

                    budgetPressure.Property(x => x.HealthcareAvailableAmount)
                       .HasPrecision(
                            precision: 18,
                            scale: 2)
                       .HasColumnName("BudgetPressureHealthcareAvailableAmount")
                       .IsRequired();

                    budgetPressure.Property(x => x.GeneralAuthorizationLevel)
                       .HasMaxLength(16)
                       .HasColumnName("BudgetPressureGeneralAuthorizationLevel")
                       .IsRequired();

                    budgetPressure.Property(x => x.OperationsAuthorizationLevel)
                       .HasMaxLength(16)
                       .HasColumnName("BudgetPressureOperationsAuthorizationLevel")
                       .IsRequired();

                    budgetPressure.Property(x => x.InfrastructureAuthorizationLevel)
                       .HasMaxLength(16)
                       .HasColumnName("BudgetPressureInfrastructureAuthorizationLevel")
                       .IsRequired();

                    budgetPressure.Property(x => x.HealthcareAuthorizationLevel)
                       .HasMaxLength(16)
                       .HasColumnName("BudgetPressureHealthcareAuthorizationLevel")
                       .IsRequired();

                    budgetPressure.Property(x => x.PressureIndex)
                       .HasPrecision(
                            precision: 5,
                            scale: 4)
                       .HasColumnName("BudgetPressureIndex")
                       .IsRequired();

                    budgetPressure.Property(x => x.EffectiveTickId)
                       .HasColumnName("BudgetPressureEffectiveTickId")
                       .IsRequired();

                    budgetPressure.Property(x => x.EffectiveAtUtc)
                       .HasColumnName("BudgetPressureEffectiveAtUtc")
                       .IsRequired();
                });

            builder.Navigation(x => x.OperationalBudgetPressure)
               .IsRequired();

            builder.Ignore(x => x.DomainEvents);

            builder.Property<uint>("xmin")
               .HasColumnName("xmin")
               .ValueGeneratedOnAddOrUpdate()
               .IsConcurrencyToken();
        }

        private static void ConfigureSystemState<TOwner>(
            OwnedNavigationBuilder<TOwner, CitySystemState> builder,
            string prefix)
            where TOwner : class
        {
            builder.Property(x => x.Kind)
               .HasConversion<int>()
               .HasColumnName($"{prefix}Kind")
               .IsRequired();

            builder.Property(x => x.LoadIndex)
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .HasColumnName($"{prefix}LoadIndex")
               .IsRequired();

            builder.Property(x => x.ServiceQualityIndex)
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .HasColumnName($"{prefix}ServiceQualityIndex")
               .IsRequired();

            builder.Property(x => x.BacklogIndex)
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .HasColumnName($"{prefix}BacklogIndex")
               .IsRequired();

            builder.Property(x => x.FailureRiskIndex)
               .HasPrecision(
                    precision: 5,
                    scale: 4)
               .HasColumnName($"{prefix}FailureRiskIndex")
               .IsRequired();
        }

        private static void ConfigurePendingOperationalWork<TOwner>(
            OwnedNavigationBuilder<TOwner, CityPendingOperationalWorkState> builder,
            string prefix)
            where TOwner : class
        {
            builder.Property(x => x.IsScheduled)
               .HasColumnName($"{prefix}IsScheduled")
               .IsRequired();

            builder.Property(x => x.Focus)
               .HasMaxLength(64)
               .HasColumnName($"{prefix}Focus")
               .IsRequired();

            builder.Property(x => x.Intensity)
               .HasMaxLength(32)
               .HasColumnName($"{prefix}Intensity")
               .IsRequired();

            builder.Property(x => x.FocusDistrictId)
               .HasColumnName($"{prefix}FocusDistrictId")
               .IsRequired(false);

            builder.Property(x => x.ReadyAtTickId)
               .HasColumnName($"{prefix}ReadyAtTickId")
               .IsRequired();
        }
    }
}
