using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
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
            CityPendingOperationalWorkState pendingDrainageMaintenance,
            CityPendingOperationalWorkState pendingSnowRemovalMaintenance,
            CityPendingOperationalWorkState pendingRoadAccessMaintenance,
            CityPendingOperationalWorkState pendingHeatingMaintenance,
            CityPendingOperationalWorkState pendingWaterDistributionMaintenance,
            CityPendingOperationalWorkState pendingSanitationMaintenance,
            CityPendingOperationalWorkState pendingPowerDistributionMaintenance,
            CityPendingOperationalWorkState pendingUtilityIncidentResponse,
            CityResourceSupplyState resourceSupply,
            CityOperationalBudgetPressureState operationalBudgetPressure,
            CityWeatherPressureProfile weatherPressure,
            FloodingIndex floodingIndex,
            SnowAccumulationIndex snowAccumulationIndex,
            RoadAccessibilityIndex roadAccessibilityIndex,
            HeatingCoverageIndex heatingCoverageIndex,
            WaterCoverageIndex waterCoverageIndex,
            SanitationCoverageIndex sanitationCoverageIndex,
            PowerCoverageIndex powerCoverageIndex,
            UtilityContinuityIndex utilityContinuityIndex,
            long lastAppliedTickId,
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
            PendingDrainageMaintenance = pendingDrainageMaintenance;
            PendingSnowRemovalMaintenance = pendingSnowRemovalMaintenance;
            PendingRoadAccessMaintenance = pendingRoadAccessMaintenance;
            PendingHeatingMaintenance = pendingHeatingMaintenance;
            PendingWaterDistributionMaintenance = pendingWaterDistributionMaintenance;
            PendingSanitationMaintenance = pendingSanitationMaintenance;
            PendingPowerDistributionMaintenance = pendingPowerDistributionMaintenance;
            PendingUtilityIncidentResponse = pendingUtilityIncidentResponse;
            ResourceSupply = resourceSupply;
            OperationalBudgetPressure = operationalBudgetPressure;
            WeatherPressure = weatherPressure;
            FloodingIndex = floodingIndex;
            SnowAccumulationIndex = snowAccumulationIndex;
            RoadAccessibilityIndex = roadAccessibilityIndex;
            HeatingCoverageIndex = heatingCoverageIndex;
            WaterCoverageIndex = waterCoverageIndex;
            SanitationCoverageIndex = sanitationCoverageIndex;
            PowerCoverageIndex = powerCoverageIndex;
            UtilityContinuityIndex = utilityContinuityIndex;
            LastAppliedTickId = EnsureTickId(
                value: lastAppliedTickId,
                propertyName: nameof(lastAppliedTickId));
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
            PendingDrainageMaintenance = null!;
            PendingSnowRemovalMaintenance = null!;
            PendingRoadAccessMaintenance = null!;
            PendingHeatingMaintenance = null!;
            PendingWaterDistributionMaintenance = null!;
            PendingSanitationMaintenance = null!;
            PendingPowerDistributionMaintenance = null!;
            PendingUtilityIncidentResponse = null!;
            ResourceSupply = null!;
            OperationalBudgetPressure = null!;
            WeatherPressure = null!;
        }

        public SimulationHostId SimulationHostId => Id;
        public CitySystemState Drainage { get; }
        public CityDrainageInfrastructureState DrainageInfrastructure { get; }
        public CitySystemState SnowRemoval { get; }
        public CitySnowRemovalInfrastructureState SnowRemovalInfrastructure { get; }
        public CitySystemState RoadAccess { get; }
        public CityRoadAccessInfrastructureState RoadAccessInfrastructure { get; }
        public CitySystemState Heating { get; }
        public CityHeatingInfrastructureState HeatingInfrastructure { get; }
        public CitySystemState WaterDistribution { get; }
        public CityWaterDistributionInfrastructureState WaterDistributionInfrastructure { get; }
        public CitySystemState Sanitation { get; }
        public CitySanitationInfrastructureState SanitationInfrastructure { get; }
        public CitySystemState PowerDistribution { get; }
        public CityPowerDistributionInfrastructureState PowerDistributionInfrastructure { get; }
        public CitySystemState UtilityIncidents { get; }
        public CityUtilityIncidentInfrastructureState UtilityIncidentInfrastructure { get; }
        public CityPendingOperationalWorkState PendingDrainageMaintenance { get; }
        public CityPendingOperationalWorkState PendingSnowRemovalMaintenance { get; }
        public CityPendingOperationalWorkState PendingRoadAccessMaintenance { get; }
        public CityPendingOperationalWorkState PendingHeatingMaintenance { get; }
        public CityPendingOperationalWorkState PendingWaterDistributionMaintenance { get; }
        public CityPendingOperationalWorkState PendingSanitationMaintenance { get; }
        public CityPendingOperationalWorkState PendingPowerDistributionMaintenance { get; }
        public CityPendingOperationalWorkState PendingUtilityIncidentResponse { get; }
        public CityResourceSupplyState ResourceSupply { get; }
        public CityOperationalBudgetPressureState OperationalBudgetPressure { get; }
        public CityWeatherPressureProfile WeatherPressure { get; private set; }
        public FloodingIndex FloodingIndex { get; private set; }
        public SnowAccumulationIndex SnowAccumulationIndex { get; private set; }
        public RoadAccessibilityIndex RoadAccessibilityIndex { get; private set; }
        public HeatingCoverageIndex HeatingCoverageIndex { get; private set; }
        public WaterCoverageIndex WaterCoverageIndex { get; private set; }
        public SanitationCoverageIndex SanitationCoverageIndex { get; private set; }
        public PowerCoverageIndex PowerCoverageIndex { get; private set; }
        public UtilityContinuityIndex UtilityContinuityIndex { get; private set; }
        public long LastAppliedTickId { get; private set; }
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
                waterDistributionInfrastructure: CityWaterDistributionInfrastructureState.Create(
                    seed.WaterDistributionInfrastructure),
                sanitation: CitySystemState.Create(seed.Sanitation),
                sanitationInfrastructure: CitySanitationInfrastructureState.Create(seed.SanitationInfrastructure),
                powerDistribution: CitySystemState.Create(seed.PowerDistribution),
                powerDistributionInfrastructure: CityPowerDistributionInfrastructureState.Create(
                    seed.PowerDistributionInfrastructure),
                utilityIncidents: CitySystemState.Create(seed.UtilityIncidents),
                utilityIncidentInfrastructure: CityUtilityIncidentInfrastructureState.Create(
                    seed.UtilityIncidentInfrastructure),
                pendingDrainageMaintenance: CityPendingOperationalWorkState.None(),
                pendingSnowRemovalMaintenance: CityPendingOperationalWorkState.None(),
                pendingRoadAccessMaintenance: CityPendingOperationalWorkState.None(),
                pendingHeatingMaintenance: CityPendingOperationalWorkState.None(),
                pendingWaterDistributionMaintenance: CityPendingOperationalWorkState.None(),
                pendingSanitationMaintenance: CityPendingOperationalWorkState.None(),
                pendingPowerDistributionMaintenance: CityPendingOperationalWorkState.None(),
                pendingUtilityIncidentResponse: CityPendingOperationalWorkState.None(),
                resourceSupply: CityResourceSupplyState.Create(seed.ResourceSupply),
                operationalBudgetPressure: CityOperationalBudgetPressureState.Create(seed.OperationalBudgetPressure),
                weatherPressure: CityWeatherPressureProfile.Neutral(),
                floodingIndex: seed.FloodingIndex,
                snowAccumulationIndex: seed.SnowAccumulationIndex,
                roadAccessibilityIndex: seed.RoadAccessibilityIndex,
                heatingCoverageIndex: seed.HeatingCoverageIndex,
                waterCoverageIndex: seed.WaterCoverageIndex,
                sanitationCoverageIndex: seed.SanitationCoverageIndex,
                powerCoverageIndex: seed.PowerCoverageIndex,
                utilityContinuityIndex: seed.UtilityContinuityIndex,
                lastAppliedTickId: 0,
                lastEvaluatedAtUtc: seed.EvaluatedAtUtc);
        }

        public void ApplyWeatherPressure(CityWeatherPressureProfile weatherPressure)
        {
            GuardHelper.AgainstNull(
                value: weatherPressure,
                errorFactory: ClassicCityDomainErrorsFactory.CityWeatherPressureProfileRequired);

            WeatherPressure = weatherPressure;
        }

        public void ApplyResourceSupply(CityResourceSupplySnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            ResourceSupply.ApplySnapshot(snapshot);
        }

        public void ApplyOperationalBudgetPressure(CityOperationalBudgetPressureSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            OperationalBudgetPressure.ApplySnapshot(snapshot);
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
            ResourceSupply.ApplySnapshot(snapshot.ResourceSupply);
            OperationalBudgetPressure.ApplySnapshot(snapshot.OperationalBudgetPressure);
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
                utilityContinuityIndex: UtilityContinuityIndex,
                resourceSupply: ResourceSupply.ToSnapshot(),
                operationalBudgetPressure: OperationalBudgetPressure.ToSnapshot());
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

        public void ScheduleDrainageMaintenance(
            DrainageMaintenanceFocus focus,
            DrainageMaintenanceIntensity intensity,
            long readyAtTickId)
        {
            PendingDrainageMaintenance.Schedule(
                focus: focus.ToString(),
                intensity: intensity.ToString(),
                focusDistrictId: null,
                readyAtTickId: EnsureTickId(
                    value: readyAtTickId,
                    propertyName: nameof(readyAtTickId)));
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

        public void ScheduleSnowRemovalMaintenance(
            SnowRemovalMaintenanceFocus focus,
            SnowRemovalMaintenanceIntensity intensity,
            long readyAtTickId)
        {
            PendingSnowRemovalMaintenance.Schedule(
                focus: focus.ToString(),
                intensity: intensity.ToString(),
                focusDistrictId: null,
                readyAtTickId: EnsureTickId(
                    value: readyAtTickId,
                    propertyName: nameof(readyAtTickId)));
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

        public void ScheduleRoadAccessMaintenance(
            RoadAccessMaintenanceFocus focus,
            RoadAccessMaintenanceIntensity intensity,
            long readyAtTickId)
        {
            PendingRoadAccessMaintenance.Schedule(
                focus: focus.ToString(),
                intensity: intensity.ToString(),
                focusDistrictId: null,
                readyAtTickId: EnsureTickId(
                    value: readyAtTickId,
                    propertyName: nameof(readyAtTickId)));
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

        public void ScheduleHeatingMaintenance(
            HeatingMaintenanceFocus focus,
            HeatingMaintenanceIntensity intensity,
            long readyAtTickId)
        {
            PendingHeatingMaintenance.Schedule(
                focus: focus.ToString(),
                intensity: intensity.ToString(),
                focusDistrictId: null,
                readyAtTickId: EnsureTickId(
                    value: readyAtTickId,
                    propertyName: nameof(readyAtTickId)));
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

        public void ScheduleWaterDistributionMaintenance(
            WaterDistributionMaintenanceFocus focus,
            WaterDistributionMaintenanceIntensity intensity,
            long readyAtTickId)
        {
            PendingWaterDistributionMaintenance.Schedule(
                focus: focus.ToString(),
                intensity: intensity.ToString(),
                focusDistrictId: null,
                readyAtTickId: EnsureTickId(
                    value: readyAtTickId,
                    propertyName: nameof(readyAtTickId)));
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

        public void ScheduleSanitationMaintenance(
            SanitationMaintenanceFocus focus,
            SanitationMaintenanceIntensity intensity,
            long readyAtTickId)
        {
            PendingSanitationMaintenance.Schedule(
                focus: focus.ToString(),
                intensity: intensity.ToString(),
                focusDistrictId: null,
                readyAtTickId: EnsureTickId(
                    value: readyAtTickId,
                    propertyName: nameof(readyAtTickId)));
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

        public void SchedulePowerDistributionMaintenance(
            PowerDistributionMaintenanceFocus focus,
            PowerDistributionMaintenanceIntensity intensity,
            long readyAtTickId)
        {
            PendingPowerDistributionMaintenance.Schedule(
                focus: focus.ToString(),
                intensity: intensity.ToString(),
                focusDistrictId: null,
                readyAtTickId: EnsureTickId(
                    value: readyAtTickId,
                    propertyName: nameof(readyAtTickId)));
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

        public void ScheduleUtilityIncidentResponse(
            UtilityIncidentResponseFocus focus,
            UtilityIncidentResponseIntensity intensity,
            Guid? focusDistrictId,
            long readyAtTickId)
        {
            PendingUtilityIncidentResponse.Schedule(
                focus: focus.ToString(),
                intensity: intensity.ToString(),
                focusDistrictId: focusDistrictId,
                readyAtTickId: EnsureTickId(
                    value: readyAtTickId,
                    propertyName: nameof(readyAtTickId)));
        }

        public bool ApplyDuePendingOperations(long tickId)
        {
            bool applied = false;

            if (PendingDrainageMaintenance.IsReady(tickId))
            {
                DispatchDrainageMaintenance(
                    focus: Enum.Parse<DrainageMaintenanceFocus>(
                        value: PendingDrainageMaintenance.Focus,
                        ignoreCase: true),
                    intensity: Enum.Parse<DrainageMaintenanceIntensity>(
                        value: PendingDrainageMaintenance.Intensity,
                        ignoreCase: true));
                PendingDrainageMaintenance.Clear();
                applied = true;
            }

            if (PendingSnowRemovalMaintenance.IsReady(tickId))
            {
                DispatchSnowRemovalMaintenance(
                    focus: Enum.Parse<SnowRemovalMaintenanceFocus>(
                        value: PendingSnowRemovalMaintenance.Focus,
                        ignoreCase: true),
                    intensity: Enum.Parse<SnowRemovalMaintenanceIntensity>(
                        value: PendingSnowRemovalMaintenance.Intensity,
                        ignoreCase: true));
                PendingSnowRemovalMaintenance.Clear();
                applied = true;
            }

            if (PendingRoadAccessMaintenance.IsReady(tickId))
            {
                DispatchRoadAccessMaintenance(
                    focus: Enum.Parse<RoadAccessMaintenanceFocus>(
                        value: PendingRoadAccessMaintenance.Focus,
                        ignoreCase: true),
                    intensity: Enum.Parse<RoadAccessMaintenanceIntensity>(
                        value: PendingRoadAccessMaintenance.Intensity,
                        ignoreCase: true));
                PendingRoadAccessMaintenance.Clear();
                applied = true;
            }

            if (PendingHeatingMaintenance.IsReady(tickId))
            {
                DispatchHeatingMaintenance(
                    focus: Enum.Parse<HeatingMaintenanceFocus>(
                        value: PendingHeatingMaintenance.Focus,
                        ignoreCase: true),
                    intensity: Enum.Parse<HeatingMaintenanceIntensity>(
                        value: PendingHeatingMaintenance.Intensity,
                        ignoreCase: true));
                PendingHeatingMaintenance.Clear();
                applied = true;
            }

            if (PendingWaterDistributionMaintenance.IsReady(tickId))
            {
                DispatchWaterDistributionMaintenance(
                    focus: Enum.Parse<WaterDistributionMaintenanceFocus>(
                        value: PendingWaterDistributionMaintenance.Focus,
                        ignoreCase: true),
                    intensity: Enum.Parse<WaterDistributionMaintenanceIntensity>(
                        value: PendingWaterDistributionMaintenance.Intensity,
                        ignoreCase: true));
                PendingWaterDistributionMaintenance.Clear();
                applied = true;
            }

            if (PendingSanitationMaintenance.IsReady(tickId))
            {
                DispatchSanitationMaintenance(
                    focus: Enum.Parse<SanitationMaintenanceFocus>(
                        value: PendingSanitationMaintenance.Focus,
                        ignoreCase: true),
                    intensity: Enum.Parse<SanitationMaintenanceIntensity>(
                        value: PendingSanitationMaintenance.Intensity,
                        ignoreCase: true));
                PendingSanitationMaintenance.Clear();
                applied = true;
            }

            if (PendingPowerDistributionMaintenance.IsReady(tickId))
            {
                DispatchPowerDistributionMaintenance(
                    focus: Enum.Parse<PowerDistributionMaintenanceFocus>(
                        value: PendingPowerDistributionMaintenance.Focus,
                        ignoreCase: true),
                    intensity: Enum.Parse<PowerDistributionMaintenanceIntensity>(
                        value: PendingPowerDistributionMaintenance.Intensity,
                        ignoreCase: true));
                PendingPowerDistributionMaintenance.Clear();
                applied = true;
            }

            if (PendingUtilityIncidentResponse.IsReady(tickId))
            {
                DispatchUtilityIncidentResponse(
                    focus: Enum.Parse<UtilityIncidentResponseFocus>(
                        value: PendingUtilityIncidentResponse.Focus,
                        ignoreCase: true),
                    intensity: Enum.Parse<UtilityIncidentResponseIntensity>(
                        value: PendingUtilityIncidentResponse.Intensity,
                        ignoreCase: true));
                PendingUtilityIncidentResponse.Clear();
                applied = true;
            }

            return applied;
        }

        public void MarkTickApplied(long tickId)
        {
            long validatedTickId = EnsureTickId(
                value: tickId,
                propertyName: nameof(tickId));

            if (validatedTickId < LastAppliedTickId)
                throw new InvalidOperationException("Environmental tick progression cannot move backwards.");

            LastAppliedTickId = validatedTickId;
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

        private static long EnsureTickId(
            long value,
            string propertyName)
        {
            return value >= 0
                ? value
                : throw new ArgumentOutOfRangeException(
                    paramName: propertyName,
                    message: "Tick identifiers cannot be negative.");
        }
    }
}
