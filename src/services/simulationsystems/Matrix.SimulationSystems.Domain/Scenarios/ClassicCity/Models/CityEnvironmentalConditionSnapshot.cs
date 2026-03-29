using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models
{
    /// <summary>
    ///     Aggregate snapshot of the first environmental systems slice for Classic City.
    /// </summary>
    public sealed class CityEnvironmentalConditionSnapshot
    {
        public CityEnvironmentalConditionSnapshot(
            CitySystemSnapshot drainage,
            CityDrainageInfrastructureSnapshot drainageInfrastructure,
            CitySystemSnapshot snowRemoval,
            CitySnowRemovalInfrastructureSnapshot snowRemovalInfrastructure,
            CitySystemSnapshot roadAccess,
            CityRoadAccessInfrastructureSnapshot roadAccessInfrastructure,
            CitySystemSnapshot heating,
            CityHeatingInfrastructureSnapshot heatingInfrastructure,
            CitySystemSnapshot waterDistribution,
            CityWaterDistributionInfrastructureSnapshot waterDistributionInfrastructure,
            CitySystemSnapshot sanitation,
            CitySanitationInfrastructureSnapshot sanitationInfrastructure,
            FloodingIndex floodingIndex,
            SnowAccumulationIndex snowAccumulationIndex,
            RoadAccessibilityIndex roadAccessibilityIndex,
            HeatingCoverageIndex heatingCoverageIndex,
            WaterCoverageIndex waterCoverageIndex,
            SanitationCoverageIndex sanitationCoverageIndex,
            DateTimeOffset evaluatedAtUtc,
            CitySystemSnapshot? powerDistribution = null,
            CityPowerDistributionInfrastructureSnapshot? powerDistributionInfrastructure = null,
            PowerCoverageIndex? powerCoverageIndex = null,
            CitySystemSnapshot? utilityIncidents = null,
            CityUtilityIncidentInfrastructureSnapshot? utilityIncidentInfrastructure = null,
            UtilityContinuityIndex? utilityContinuityIndex = null,
            CityResourceSupplySnapshot? resourceSupply = null,
            CityOperationalBudgetPressureSnapshot? operationalBudgetPressure = null)
        {
            Drainage = GuardHelper.AgainstNull(
                value: drainage,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSystemSnapshotRequired(
                    systemName: "drainage",
                    propertyName: nameof(drainage)));
            DrainageInfrastructure = GuardHelper.AgainstNull(
                value: drainageInfrastructure,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSnapshotRequired(
                    propertyName: nameof(drainageInfrastructure)));
            SnowRemoval = GuardHelper.AgainstNull(
                value: snowRemoval,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSystemSnapshotRequired(
                    systemName: "snowRemoval",
                    propertyName: nameof(snowRemoval)));
            SnowRemovalInfrastructure = GuardHelper.AgainstNull(
                value: snowRemovalInfrastructure,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSnapshotRequired(
                    propertyName: nameof(snowRemovalInfrastructure)));
            RoadAccess = GuardHelper.AgainstNull(
                value: roadAccess,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSystemSnapshotRequired(
                    systemName: "roadAccess",
                    propertyName: nameof(roadAccess)));
            RoadAccessInfrastructure = GuardHelper.AgainstNull(
                value: roadAccessInfrastructure,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSnapshotRequired(
                    propertyName: nameof(roadAccessInfrastructure)));
            Heating = GuardHelper.AgainstNull(
                value: heating,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSystemSnapshotRequired(
                    systemName: "heating",
                    propertyName: nameof(heating)));
            HeatingInfrastructure = GuardHelper.AgainstNull(
                value: heatingInfrastructure,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSnapshotRequired(
                    propertyName: nameof(heatingInfrastructure)));
            WaterDistribution = GuardHelper.AgainstNull(
                value: waterDistribution,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSystemSnapshotRequired(
                    systemName: "waterDistribution",
                    propertyName: nameof(waterDistribution)));
            WaterDistributionInfrastructure = GuardHelper.AgainstNull(
                value: waterDistributionInfrastructure,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSnapshotRequired(
                    propertyName: nameof(waterDistributionInfrastructure)));
            Sanitation = GuardHelper.AgainstNull(
                value: sanitation,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSystemSnapshotRequired(
                    systemName: "sanitation",
                    propertyName: nameof(sanitation)));
            SanitationInfrastructure = GuardHelper.AgainstNull(
                value: sanitationInfrastructure,
                errorFactory: _ => ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSnapshotRequired(
                    propertyName: nameof(sanitationInfrastructure)));
            PowerDistribution = powerDistribution ?? CreateDefaultPowerDistribution();
            PowerDistributionInfrastructure = powerDistributionInfrastructure ?? CreateDefaultPowerDistributionInfrastructure();
            UtilityIncidents = utilityIncidents ?? CreateDefaultUtilityIncidents();
            UtilityIncidentInfrastructure = utilityIncidentInfrastructure ?? CreateDefaultUtilityIncidentInfrastructure();
            FloodingIndex = floodingIndex;
            SnowAccumulationIndex = snowAccumulationIndex;
            RoadAccessibilityIndex = roadAccessibilityIndex;
            HeatingCoverageIndex = heatingCoverageIndex;
            WaterCoverageIndex = waterCoverageIndex;
            SanitationCoverageIndex = sanitationCoverageIndex;
            PowerCoverageIndex = powerCoverageIndex ?? PowerCoverageIndex.From(1m);
            UtilityContinuityIndex = utilityContinuityIndex ?? UtilityContinuityIndex.From(1m);
            ResourceSupply = resourceSupply ?? CityResourceSupplySnapshot.Neutral(evaluatedAtUtc);
            OperationalBudgetPressure = operationalBudgetPressure ?? CityOperationalBudgetPressureSnapshot.Neutral(evaluatedAtUtc);
            EvaluatedAtUtc = EnsureUtc(
                value: evaluatedAtUtc,
                paramName: nameof(evaluatedAtUtc));
        }

        public CitySystemSnapshot Drainage { get; }
        public CityDrainageInfrastructureSnapshot DrainageInfrastructure { get; }
        public CitySystemSnapshot SnowRemoval { get; }
        public CitySnowRemovalInfrastructureSnapshot SnowRemovalInfrastructure { get; }
        public CitySystemSnapshot RoadAccess { get; }
        public CityRoadAccessInfrastructureSnapshot RoadAccessInfrastructure { get; }
        public CitySystemSnapshot Heating { get; }
        public CityHeatingInfrastructureSnapshot HeatingInfrastructure { get; }
        public CitySystemSnapshot WaterDistribution { get; }
        public CityWaterDistributionInfrastructureSnapshot WaterDistributionInfrastructure { get; }
        public CitySystemSnapshot Sanitation { get; }
        public CitySanitationInfrastructureSnapshot SanitationInfrastructure { get; }
        public CitySystemSnapshot PowerDistribution { get; }
        public CityPowerDistributionInfrastructureSnapshot PowerDistributionInfrastructure { get; }
        public CitySystemSnapshot UtilityIncidents { get; }
        public CityUtilityIncidentInfrastructureSnapshot UtilityIncidentInfrastructure { get; }
        public FloodingIndex FloodingIndex { get; }
        public SnowAccumulationIndex SnowAccumulationIndex { get; }
        public RoadAccessibilityIndex RoadAccessibilityIndex { get; }
        public HeatingCoverageIndex HeatingCoverageIndex { get; }
        public WaterCoverageIndex WaterCoverageIndex { get; }
        public SanitationCoverageIndex SanitationCoverageIndex { get; }
        public PowerCoverageIndex PowerCoverageIndex { get; }
        public UtilityContinuityIndex UtilityContinuityIndex { get; }
        public CityResourceSupplySnapshot ResourceSupply { get; }
        public CityOperationalBudgetPressureSnapshot OperationalBudgetPressure { get; }
        public DateTimeOffset EvaluatedAtUtc { get; }

        private static CitySystemSnapshot CreateDefaultPowerDistribution()
        {
            return new CitySystemSnapshot(
                kind: CitySystemKind.PowerDistribution,
                loadIndex: 0m,
                serviceQualityIndex: 1m,
                backlogIndex: 0m,
                failureRiskIndex: 0m);
        }

        private static CityPowerDistributionInfrastructureSnapshot CreateDefaultPowerDistributionInfrastructure()
        {
            return new CityPowerDistributionInfrastructureSnapshot(
                substationCapacityIndex: 1m,
                gridIntegrityIndex: 1m,
                switchingReadinessIndex: 1m,
                crewReadinessIndex: 1m,
                incidentPressureIndex: 0m,
                emergencyModeEnabled: false);
        }

        private static CitySystemSnapshot CreateDefaultUtilityIncidents()
        {
            return new CitySystemSnapshot(
                kind: CitySystemKind.UtilityIncidents,
                loadIndex: 0m,
                serviceQualityIndex: 1m,
                backlogIndex: 0m,
                failureRiskIndex: 0m);
        }

        private static CityUtilityIncidentInfrastructureSnapshot CreateDefaultUtilityIncidentInfrastructure()
        {
            return new CityUtilityIncidentInfrastructureSnapshot(
                dispatchReadinessIndex: 1m,
                restorationCoverageIndex: 1m,
                spareCapacityIndex: 1m,
                fieldCoordinationIndex: 1m,
                incidentQueuePressureIndex: 0m,
                emergencyModeEnabled: false);
        }

        private static DateTimeOffset EnsureUtc(
            DateTimeOffset value,
            string paramName)
        {
            return value.Offset == TimeSpan.Zero
                ? value
                : throw ClassicCityDomainErrorsFactory.CityEnvironmentalTimestampMustBeUtc(
                    value: value,
                    propertyName: paramName);
        }
    }
}
