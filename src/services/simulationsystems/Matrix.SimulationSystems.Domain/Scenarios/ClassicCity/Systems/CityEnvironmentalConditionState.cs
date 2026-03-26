using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.SimulationSystems.Domain.Simulation;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems
{
    /// <summary>
    ///     Aggregate root for physical city conditions driven by weather pressure and system response.
    /// </summary>
    public sealed class CityEnvironmentalConditionState : AggregateRoot<SimulationHostId>
    {
        private CityEnvironmentalConditionState(
            SimulationHostId simulationHostId,
            CitySystemState drainage,
            CityDrainageInfrastructureState drainageInfrastructure,
            CitySystemState snowRemoval,
            CitySnowRemovalInfrastructureState snowRemovalInfrastructure,
            CitySystemState roadAccess,
            CityRoadAccessInfrastructureState roadAccessInfrastructure,
            CitySystemState heating,
            CityHeatingInfrastructureState heatingInfrastructure,
            CitySystemState waterDistribution,
            CityWaterDistributionInfrastructureState waterDistributionInfrastructure,
            CitySystemState sanitation,
            CitySanitationInfrastructureState sanitationInfrastructure,
            CitySystemState powerDistribution,
            CityPowerDistributionInfrastructureState powerDistributionInfrastructure,
            CitySystemState utilityIncidents,
            CityUtilityIncidentInfrastructureState utilityIncidentInfrastructure,
            CityWeatherPressureProfile weatherPressure,
            FloodingIndex floodingIndex,
            SnowAccumulationIndex snowAccumulationIndex,
            RoadAccessibilityIndex roadAccessibilityIndex,
            HeatingCoverageIndex heatingCoverageIndex,
            WaterCoverageIndex waterCoverageIndex,
            SanitationCoverageIndex sanitationCoverageIndex,
            PowerCoverageIndex powerCoverageIndex,
            UtilityContinuityIndex utilityContinuityIndex,
            DateTimeOffset lastEvaluatedAtUtc)
            : base(simulationHostId)
        {
            Drainage = drainage;
            DrainageInfrastructure = drainageInfrastructure;
            SnowRemoval = snowRemoval;
            SnowRemovalInfrastructure = snowRemovalInfrastructure;
            RoadAccess = roadAccess;
            RoadAccessInfrastructure = roadAccessInfrastructure;
            Heating = heating;
            HeatingInfrastructure = heatingInfrastructure;
            WaterDistribution = waterDistribution;
            WaterDistributionInfrastructure = waterDistributionInfrastructure;
            Sanitation = sanitation;
            SanitationInfrastructure = sanitationInfrastructure;
            PowerDistribution = powerDistribution;
            PowerDistributionInfrastructure = powerDistributionInfrastructure;
            UtilityIncidents = utilityIncidents;
            UtilityIncidentInfrastructure = utilityIncidentInfrastructure;
            WeatherPressure = weatherPressure;
            FloodingIndex = floodingIndex;
            SnowAccumulationIndex = snowAccumulationIndex;
            RoadAccessibilityIndex = roadAccessibilityIndex;
            HeatingCoverageIndex = heatingCoverageIndex;
            WaterCoverageIndex = waterCoverageIndex;
            SanitationCoverageIndex = sanitationCoverageIndex;
            PowerCoverageIndex = powerCoverageIndex;
            UtilityContinuityIndex = utilityContinuityIndex;
            LastEvaluatedAtUtc = EnsureUtc(
                value: lastEvaluatedAtUtc,
                paramName: nameof(lastEvaluatedAtUtc));
        }

        private CityEnvironmentalConditionState()
            : base(default(SimulationHostId))
        {
            Drainage = null!;
            DrainageInfrastructure = null!;
            SnowRemoval = null!;
            SnowRemovalInfrastructure = null!;
            RoadAccess = null!;
            RoadAccessInfrastructure = null!;
            Heating = null!;
            HeatingInfrastructure = null!;
            WaterDistribution = null!;
            WaterDistributionInfrastructure = null!;
            Sanitation = null!;
            SanitationInfrastructure = null!;
            PowerDistribution = null!;
            PowerDistributionInfrastructure = null!;
            UtilityIncidents = null!;
            UtilityIncidentInfrastructure = null!;
            WeatherPressure = null!;
        }

        public SimulationHostId SimulationHostId => Id;
        public CitySystemState Drainage { get; private set; }
        public CityDrainageInfrastructureState DrainageInfrastructure { get; private set; }
        public CitySystemState SnowRemoval { get; private set; }
        public CitySnowRemovalInfrastructureState SnowRemovalInfrastructure { get; private set; }
        public CitySystemState RoadAccess { get; private set; }
        public CityRoadAccessInfrastructureState RoadAccessInfrastructure { get; private set; }
        public CitySystemState Heating { get; private set; }
        public CityHeatingInfrastructureState HeatingInfrastructure { get; private set; }
        public CitySystemState WaterDistribution { get; private set; }
        public CityWaterDistributionInfrastructureState WaterDistributionInfrastructure { get; private set; }
        public CitySystemState Sanitation { get; private set; }
        public CitySanitationInfrastructureState SanitationInfrastructure { get; private set; }
        public CitySystemState PowerDistribution { get; private set; }
        public CityPowerDistributionInfrastructureState PowerDistributionInfrastructure { get; private set; }
        public CitySystemState UtilityIncidents { get; private set; }
        public CityUtilityIncidentInfrastructureState UtilityIncidentInfrastructure { get; private set; }
        public CityWeatherPressureProfile WeatherPressure { get; private set; }
        public FloodingIndex FloodingIndex { get; private set; }
        public SnowAccumulationIndex SnowAccumulationIndex { get; private set; }
        public RoadAccessibilityIndex RoadAccessibilityIndex { get; private set; }
        public HeatingCoverageIndex HeatingCoverageIndex { get; private set; }
        public WaterCoverageIndex WaterCoverageIndex { get; private set; }
        public SanitationCoverageIndex SanitationCoverageIndex { get; private set; }
        public PowerCoverageIndex PowerCoverageIndex { get; private set; }
        public UtilityContinuityIndex UtilityContinuityIndex { get; private set; }
        public DateTimeOffset LastEvaluatedAtUtc { get; private set; }

        public static CityEnvironmentalConditionState Create(
            SimulationHostId simulationHostId,
            CityEnvironmentalConditionSnapshot seed)
        {
            GuardHelper.AgainstNull(
                value: seed,
                errorFactory: ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSnapshotRequired);

            return new CityEnvironmentalConditionState(
                simulationHostId: simulationHostId,
                drainage: CitySystemState.Create(seed.Drainage),
                drainageInfrastructure: CityDrainageInfrastructureState.Create(seed.DrainageInfrastructure),
                snowRemoval: CitySystemState.Create(seed.SnowRemoval),
                snowRemovalInfrastructure: CitySnowRemovalInfrastructureState.Create(seed.SnowRemovalInfrastructure),
                roadAccess: CitySystemState.Create(seed.RoadAccess),
                roadAccessInfrastructure: CityRoadAccessInfrastructureState.Create(seed.RoadAccessInfrastructure),
                heating: CitySystemState.Create(seed.Heating),
                heatingInfrastructure: CityHeatingInfrastructureState.Create(seed.HeatingInfrastructure),
                waterDistribution: CitySystemState.Create(seed.WaterDistribution),
                waterDistributionInfrastructure: CityWaterDistributionInfrastructureState.Create(seed.WaterDistributionInfrastructure),
                sanitation: CitySystemState.Create(seed.Sanitation),
                sanitationInfrastructure: CitySanitationInfrastructureState.Create(seed.SanitationInfrastructure),
                powerDistribution: CitySystemState.Create(seed.PowerDistribution),
                powerDistributionInfrastructure: CityPowerDistributionInfrastructureState.Create(seed.PowerDistributionInfrastructure),
                utilityIncidents: CitySystemState.Create(seed.UtilityIncidents),
                utilityIncidentInfrastructure: CityUtilityIncidentInfrastructureState.Create(seed.UtilityIncidentInfrastructure),
                weatherPressure: CityWeatherPressureProfile.Neutral(),
                floodingIndex: seed.FloodingIndex,
                snowAccumulationIndex: seed.SnowAccumulationIndex,
                roadAccessibilityIndex: seed.RoadAccessibilityIndex,
                heatingCoverageIndex: seed.HeatingCoverageIndex,
                waterCoverageIndex: seed.WaterCoverageIndex,
                sanitationCoverageIndex: seed.SanitationCoverageIndex,
                powerCoverageIndex: seed.PowerCoverageIndex,
                utilityContinuityIndex: seed.UtilityContinuityIndex,
                lastEvaluatedAtUtc: seed.EvaluatedAtUtc);
        }

        public void ApplyWeatherPressure(CityWeatherPressureProfile weatherPressure)
        {
            GuardHelper.AgainstNull(
                value: weatherPressure,
                errorFactory: ClassicCityDomainErrorsFactory.CityWeatherPressureProfileRequired);

            WeatherPressure = weatherPressure;
        }

        public void ApplySnapshot(CityEnvironmentalConditionSnapshot snapshot)
        {
            GuardHelper.AgainstNull(
                value: snapshot,
                errorFactory: ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSnapshotRequired);

            if (snapshot.EvaluatedAtUtc < LastEvaluatedAtUtc)
                throw ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSnapshotCannotMoveBackwards(
                    value: snapshot.EvaluatedAtUtc,
                    previous: LastEvaluatedAtUtc,
                    propertyName: nameof(snapshot));

            Drainage.ApplySnapshot(snapshot.Drainage);
            DrainageInfrastructure.ApplySnapshot(snapshot.DrainageInfrastructure);
            SnowRemoval.ApplySnapshot(snapshot.SnowRemoval);
            SnowRemovalInfrastructure.ApplySnapshot(snapshot.SnowRemovalInfrastructure);
            RoadAccess.ApplySnapshot(snapshot.RoadAccess);
            RoadAccessInfrastructure.ApplySnapshot(snapshot.RoadAccessInfrastructure);
            Heating.ApplySnapshot(snapshot.Heating);
            HeatingInfrastructure.ApplySnapshot(snapshot.HeatingInfrastructure);
            WaterDistribution.ApplySnapshot(snapshot.WaterDistribution);
            WaterDistributionInfrastructure.ApplySnapshot(snapshot.WaterDistributionInfrastructure);
            Sanitation.ApplySnapshot(snapshot.Sanitation);
            SanitationInfrastructure.ApplySnapshot(snapshot.SanitationInfrastructure);
            PowerDistribution.ApplySnapshot(snapshot.PowerDistribution);
            PowerDistributionInfrastructure.ApplySnapshot(snapshot.PowerDistributionInfrastructure);
            UtilityIncidents.ApplySnapshot(snapshot.UtilityIncidents);
            UtilityIncidentInfrastructure.ApplySnapshot(snapshot.UtilityIncidentInfrastructure);
            FloodingIndex = snapshot.FloodingIndex;
            SnowAccumulationIndex = snapshot.SnowAccumulationIndex;
            RoadAccessibilityIndex = snapshot.RoadAccessibilityIndex;
            HeatingCoverageIndex = snapshot.HeatingCoverageIndex;
            WaterCoverageIndex = snapshot.WaterCoverageIndex;
            SanitationCoverageIndex = snapshot.SanitationCoverageIndex;
            PowerCoverageIndex = snapshot.PowerCoverageIndex;
            UtilityContinuityIndex = snapshot.UtilityContinuityIndex;
            LastEvaluatedAtUtc = snapshot.EvaluatedAtUtc;
        }

        public CityEnvironmentalConditionSnapshot ToSnapshot()
        {
            return new CityEnvironmentalConditionSnapshot(
                drainage: Drainage.ToSnapshot(),
                drainageInfrastructure: DrainageInfrastructure.ToSnapshot(),
                snowRemoval: SnowRemoval.ToSnapshot(),
                snowRemovalInfrastructure: SnowRemovalInfrastructure.ToSnapshot(),
                roadAccess: RoadAccess.ToSnapshot(),
                roadAccessInfrastructure: RoadAccessInfrastructure.ToSnapshot(),
                heating: Heating.ToSnapshot(),
                heatingInfrastructure: HeatingInfrastructure.ToSnapshot(),
                waterDistribution: WaterDistribution.ToSnapshot(),
                waterDistributionInfrastructure: WaterDistributionInfrastructure.ToSnapshot(),
                sanitation: Sanitation.ToSnapshot(),
                sanitationInfrastructure: SanitationInfrastructure.ToSnapshot(),
                floodingIndex: FloodingIndex,
                snowAccumulationIndex: SnowAccumulationIndex,
                roadAccessibilityIndex: RoadAccessibilityIndex,
                heatingCoverageIndex: HeatingCoverageIndex,
                waterCoverageIndex: WaterCoverageIndex,
                sanitationCoverageIndex: SanitationCoverageIndex,
                evaluatedAtUtc: LastEvaluatedAtUtc,
                powerDistribution: PowerDistribution.ToSnapshot(),
                powerDistributionInfrastructure: PowerDistributionInfrastructure.ToSnapshot(),
                powerCoverageIndex: PowerCoverageIndex,
                utilityIncidents: UtilityIncidents.ToSnapshot(),
                utilityIncidentInfrastructure: UtilityIncidentInfrastructure.ToSnapshot(),
                utilityContinuityIndex: UtilityContinuityIndex);
        }

        public void SetDrainageEmergencyMode(bool enabled)
        {
            DrainageInfrastructure.SetEmergencyMode(enabled);
        }

        public void DispatchDrainageMaintenance(
            DrainageMaintenanceFocus focus,
            DrainageMaintenanceIntensity intensity)
        {
            DrainageInfrastructure.DispatchMaintenance(
                focus: focus,
                intensity: intensity);
        }

        public void SetSnowRemovalEmergencyMode(bool enabled)
        {
            SnowRemovalInfrastructure.SetEmergencyMode(enabled);
        }

        public void DispatchSnowRemovalMaintenance(
            SnowRemovalMaintenanceFocus focus,
            SnowRemovalMaintenanceIntensity intensity)
        {
            SnowRemovalInfrastructure.DispatchMaintenance(
                focus: focus,
                intensity: intensity);
        }

        public void SetRoadAccessEmergencyMode(bool enabled)
        {
            RoadAccessInfrastructure.SetEmergencyMode(enabled);
        }

        public void DispatchRoadAccessMaintenance(
            RoadAccessMaintenanceFocus focus,
            RoadAccessMaintenanceIntensity intensity)
        {
            RoadAccessInfrastructure.DispatchMaintenance(
                focus: focus,
                intensity: intensity);
        }

        public void SetHeatingEmergencyMode(bool enabled)
        {
            HeatingInfrastructure.SetEmergencyMode(enabled);
        }

        public void DispatchHeatingMaintenance(
            HeatingMaintenanceFocus focus,
            HeatingMaintenanceIntensity intensity)
        {
            HeatingInfrastructure.DispatchMaintenance(
                focus: focus,
                intensity: intensity);
        }

        public void SetWaterDistributionEmergencyMode(bool enabled)
        {
            WaterDistributionInfrastructure.SetEmergencyMode(enabled);
        }

        public void DispatchWaterDistributionMaintenance(
            WaterDistributionMaintenanceFocus focus,
            WaterDistributionMaintenanceIntensity intensity)
        {
            WaterDistributionInfrastructure.DispatchMaintenance(
                focus: focus,
                intensity: intensity);
        }

        public void SetSanitationEmergencyMode(bool enabled)
        {
            SanitationInfrastructure.SetEmergencyMode(enabled);
        }

        public void DispatchSanitationMaintenance(
            SanitationMaintenanceFocus focus,
            SanitationMaintenanceIntensity intensity)
        {
            SanitationInfrastructure.DispatchMaintenance(
                focus: focus,
                intensity: intensity);
        }

        public void SetPowerDistributionEmergencyMode(bool enabled)
        {
            PowerDistributionInfrastructure.SetEmergencyMode(enabled);
        }

        public void DispatchPowerDistributionMaintenance(
            PowerDistributionMaintenanceFocus focus,
            PowerDistributionMaintenanceIntensity intensity)
        {
            PowerDistributionInfrastructure.DispatchMaintenance(
                focus: focus,
                intensity: intensity);
        }

        public void SetUtilityIncidentEmergencyMode(bool enabled)
        {
            UtilityIncidentInfrastructure.SetEmergencyMode(enabled);
        }

        public void DispatchUtilityIncidentResponse(
            UtilityIncidentResponseFocus focus,
            UtilityIncidentResponseIntensity intensity)
        {
            UtilityIncidentInfrastructure.DispatchResponse(
                focus: focus,
                intensity: intensity);
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
