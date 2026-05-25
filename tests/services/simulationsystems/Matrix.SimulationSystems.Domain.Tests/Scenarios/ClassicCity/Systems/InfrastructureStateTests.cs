using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Domain.Tests.Scenarios.ClassicCity.Systems
{
    public sealed class InfrastructureStateTests
    {
        [Fact]
        public void DrainageState_CreateApplyAndDispatchMutateState()
        {
            var state = CityDrainageInfrastructureState.Create(
                new CityDrainageInfrastructureSnapshot(
                    pumpCapacityIndex: 0.4m,
                    networkIntegrityIndex: 0.5m,
                    blockageIndex: 0.6m,
                    crewReadinessIndex: 0.7m,
                    incidentPressureIndex: 0.8m,
                    emergencyModeEnabled: false));

            state.SetEmergencyMode(true);
            state.DispatchMaintenance(
                focus: DrainageMaintenanceFocus.PumpRepairs,
                intensity: DrainageMaintenanceIntensity.Heavy);
            state.ApplySnapshot(
                new CityDrainageInfrastructureSnapshot(
                    pumpCapacityIndex: 0.3m,
                    networkIntegrityIndex: 0.4m,
                    blockageIndex: 0.5m,
                    crewReadinessIndex: 0.6m,
                    incidentPressureIndex: 0.7m,
                    emergencyModeEnabled: false));

            CityDrainageInfrastructureSnapshot snapshot = state.ToSnapshot();
            Assert.Equal(
                expected: 0.3m,
                actual: snapshot.PumpCapacityIndex);
            Assert.Equal(
                expected: 0.7m,
                actual: snapshot.IncidentPressureIndex);
            Assert.False(snapshot.EmergencyModeEnabled);
            Assert.Throws<ArgumentOutOfRangeException>(() => state.DispatchMaintenance(
                focus: (DrainageMaintenanceFocus)999,
                intensity: DrainageMaintenanceIntensity.Standard));
        }

        [Fact]
        public void MunicipalInfrastructureStates_DispatchMaintenanceMovesMetrics()
        {
            var snow = CitySnowRemovalInfrastructureState.Create(
                new CitySnowRemovalInfrastructureSnapshot(
                    fleetAvailabilityIndex: 0.5m,
                    routeCoverageIndex: 0.5m,
                    deicingReadinessIndex: 0.5m,
                    crewReadinessIndex: 0.5m,
                    incidentPressureIndex: 0.5m,
                    emergencyModeEnabled: false));
            var road = CityRoadAccessInfrastructureState.Create(
                new CityRoadAccessInfrastructureSnapshot(
                    corridorAvailabilityIndex: 0.5m,
                    surfaceIntegrityIndex: 0.5m,
                    trafficControlReadinessIndex: 0.5m,
                    crewReadinessIndex: 0.5m,
                    incidentPressureIndex: 0.5m,
                    emergencyModeEnabled: false));
            var heating = CityHeatingInfrastructureState.Create(
                new CityHeatingInfrastructureSnapshot(
                    plantCapacityIndex: 0.5m,
                    networkIntegrityIndex: 0.5m,
                    controlReadinessIndex: 0.5m,
                    crewReadinessIndex: 0.5m,
                    incidentPressureIndex: 0.5m,
                    emergencyModeEnabled: false));

            snow.DispatchMaintenance(
                focus: SnowRemovalMaintenanceFocus.RouteClearance,
                intensity: SnowRemovalMaintenanceIntensity.Heavy);
            road.DispatchMaintenance(
                focus: RoadAccessMaintenanceFocus.TrafficControl,
                intensity: RoadAccessMaintenanceIntensity.Standard);
            heating.DispatchMaintenance(
                focus: HeatingMaintenanceFocus.PlantRepairs,
                intensity: HeatingMaintenanceIntensity.Light);

            Assert.True(snow.RouteCoverageIndex > 0.5m);
            Assert.True(road.TrafficControlReadinessIndex > 0.5m);
            Assert.True(heating.PlantCapacityIndex > 0.5m);
            Assert.True(snow.IncidentPressureIndex < 0.5m);
            Assert.True(road.IncidentPressureIndex < 0.5m);
            Assert.True(heating.IncidentPressureIndex < 0.5m);
        }

        [Fact]
        public void UtilityInfrastructureStates_CreateApplyAndDispatchMutateState()
        {
            var water = CityWaterDistributionInfrastructureState.Create(
                new CityWaterDistributionInfrastructureSnapshot(
                    treatmentCapacityIndex: 0.5m,
                    networkIntegrityIndex: 0.5m,
                    pumpReadinessIndex: 0.5m,
                    crewReadinessIndex: 0.5m,
                    incidentPressureIndex: 0.5m,
                    emergencyModeEnabled: false));
            var sanitation = CitySanitationInfrastructureState.Create(
                new CitySanitationInfrastructureSnapshot(
                    treatmentStabilityIndex: 0.5m,
                    networkIntegrityIndex: 0.5m,
                    overflowControlIndex: 0.5m,
                    crewReadinessIndex: 0.5m,
                    incidentPressureIndex: 0.5m,
                    emergencyModeEnabled: false));
            var power = CityPowerDistributionInfrastructureState.Create(
                new CityPowerDistributionInfrastructureSnapshot(
                    substationCapacityIndex: 0.5m,
                    gridIntegrityIndex: 0.5m,
                    switchingReadinessIndex: 0.5m,
                    crewReadinessIndex: 0.5m,
                    incidentPressureIndex: 0.5m,
                    emergencyModeEnabled: false));
            var incidents = CityUtilityIncidentInfrastructureState.Create(
                new CityUtilityIncidentInfrastructureSnapshot(
                    dispatchReadinessIndex: 0.5m,
                    restorationCoverageIndex: 0.5m,
                    spareCapacityIndex: 0.5m,
                    fieldCoordinationIndex: 0.5m,
                    incidentQueuePressureIndex: 0.5m,
                    emergencyModeEnabled: false));

            water.DispatchMaintenance(
                focus: WaterDistributionMaintenanceFocus.PumpRecovery,
                intensity: WaterDistributionMaintenanceIntensity.Heavy);
            sanitation.DispatchMaintenance(
                focus: SanitationMaintenanceFocus.OverflowControl,
                intensity: SanitationMaintenanceIntensity.Standard);
            power.DispatchMaintenance(
                focus: PowerDistributionMaintenanceFocus.GridStabilization,
                intensity: PowerDistributionMaintenanceIntensity.Light);
            incidents.DispatchResponse(
                focus: UtilityIncidentResponseFocus.PowerOutages,
                intensity: UtilityIncidentResponseIntensity.Heavy);

            Assert.True(water.PumpReadinessIndex > 0.5m);
            Assert.True(sanitation.OverflowControlIndex > 0.5m);
            Assert.True(power.GridIntegrityIndex > 0.5m);
            Assert.True(incidents.RestorationCoverageIndex > 0.5m);
            Assert.True(incidents.IncidentQueuePressureIndex < 0.5m);

            water.ApplySnapshot(
                new CityWaterDistributionInfrastructureSnapshot(
                    treatmentCapacityIndex: 0.2m,
                    networkIntegrityIndex: 0.3m,
                    pumpReadinessIndex: 0.4m,
                    crewReadinessIndex: 0.5m,
                    incidentPressureIndex: 0.6m,
                    emergencyModeEnabled: true));
            Assert.True(water.EmergencyModeEnabled);
            Assert.Equal(
                expected: 0.2m,
                actual: water.TreatmentCapacityIndex);
        }

        [Fact]
        public void PendingOperationalWorkState_SupportsScheduleReadyAndClear()
        {
            var state = CityPendingOperationalWorkState.None();
            var districtId = Guid.Parse("73000000-0000-0000-0000-0000000000ab");

            state.Schedule(
                focus: "Balanced",
                intensity: "Standard",
                focusDistrictId: districtId,
                readyAtTickId: -5);

            Assert.True(state.IsScheduled);
            Assert.Equal(
                expected: "Balanced",
                actual: state.Focus);
            Assert.Equal(
                expected: "Standard",
                actual: state.Intensity);
            Assert.Equal(
                expected: districtId,
                actual: state.FocusDistrictId);
            Assert.Equal(
                expected: 0,
                actual: state.ReadyAtTickId);
            Assert.True(state.IsReady(0));

            state.Clear();

            Assert.False(state.IsScheduled);
            Assert.Equal(
                expected: string.Empty,
                actual: state.Focus);
            Assert.Null(state.FocusDistrictId);
            Assert.Equal(
                expected: 0,
                actual: state.ReadyAtTickId);
        }
    }
}
