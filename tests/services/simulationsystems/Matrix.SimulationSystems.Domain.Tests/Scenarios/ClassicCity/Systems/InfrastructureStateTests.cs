using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Domain.Tests.Scenarios.ClassicCity.Systems;

public sealed class InfrastructureStateTests
{
    [Fact]
    public void DrainageState_CreateApplyAndDispatchMutateState()
    {
        var state = CityDrainageInfrastructureState.Create(
            new CityDrainageInfrastructureSnapshot(0.4m, 0.5m, 0.6m, 0.7m, 0.8m, false));

        state.SetEmergencyMode(true);
        state.DispatchMaintenance(DrainageMaintenanceFocus.PumpRepairs, DrainageMaintenanceIntensity.Heavy);
        state.ApplySnapshot(new CityDrainageInfrastructureSnapshot(0.3m, 0.4m, 0.5m, 0.6m, 0.7m, false));

        CityDrainageInfrastructureSnapshot snapshot = state.ToSnapshot();
        Assert.Equal(0.3m, snapshot.PumpCapacityIndex);
        Assert.Equal(0.7m, snapshot.IncidentPressureIndex);
        Assert.False(snapshot.EmergencyModeEnabled);
        Assert.Throws<ArgumentOutOfRangeException>(() => state.DispatchMaintenance((DrainageMaintenanceFocus)999, DrainageMaintenanceIntensity.Standard));
    }

    [Fact]
    public void MunicipalInfrastructureStates_DispatchMaintenanceMovesMetrics()
    {
        var snow = CitySnowRemovalInfrastructureState.Create(new CitySnowRemovalInfrastructureSnapshot(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, false));
        var road = CityRoadAccessInfrastructureState.Create(new CityRoadAccessInfrastructureSnapshot(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, false));
        var heating = CityHeatingInfrastructureState.Create(new CityHeatingInfrastructureSnapshot(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, false));

        snow.DispatchMaintenance(SnowRemovalMaintenanceFocus.RouteClearance, SnowRemovalMaintenanceIntensity.Heavy);
        road.DispatchMaintenance(RoadAccessMaintenanceFocus.TrafficControl, RoadAccessMaintenanceIntensity.Standard);
        heating.DispatchMaintenance(HeatingMaintenanceFocus.PlantRepairs, HeatingMaintenanceIntensity.Light);

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
        var water = CityWaterDistributionInfrastructureState.Create(new CityWaterDistributionInfrastructureSnapshot(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, false));
        var sanitation = CitySanitationInfrastructureState.Create(new CitySanitationInfrastructureSnapshot(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, false));
        var power = CityPowerDistributionInfrastructureState.Create(new CityPowerDistributionInfrastructureSnapshot(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, false));
        var incidents = CityUtilityIncidentInfrastructureState.Create(new CityUtilityIncidentInfrastructureSnapshot(0.5m, 0.5m, 0.5m, 0.5m, 0.5m, false));

        water.DispatchMaintenance(WaterDistributionMaintenanceFocus.PumpRecovery, WaterDistributionMaintenanceIntensity.Heavy);
        sanitation.DispatchMaintenance(SanitationMaintenanceFocus.OverflowControl, SanitationMaintenanceIntensity.Standard);
        power.DispatchMaintenance(PowerDistributionMaintenanceFocus.GridStabilization, PowerDistributionMaintenanceIntensity.Light);
        incidents.DispatchResponse(UtilityIncidentResponseFocus.PowerOutages, UtilityIncidentResponseIntensity.Heavy);

        Assert.True(water.PumpReadinessIndex > 0.5m);
        Assert.True(sanitation.OverflowControlIndex > 0.5m);
        Assert.True(power.GridIntegrityIndex > 0.5m);
        Assert.True(incidents.RestorationCoverageIndex > 0.5m);
        Assert.True(incidents.IncidentQueuePressureIndex < 0.5m);

        water.ApplySnapshot(new CityWaterDistributionInfrastructureSnapshot(0.2m, 0.3m, 0.4m, 0.5m, 0.6m, true));
        Assert.True(water.EmergencyModeEnabled);
        Assert.Equal(0.2m, water.TreatmentCapacityIndex);
    }

    [Fact]
    public void PendingOperationalWorkState_SupportsScheduleReadyAndClear()
    {
        CityPendingOperationalWorkState state = CityPendingOperationalWorkState.None();
        Guid districtId = Guid.Parse("73000000-0000-0000-0000-0000000000ab");

        state.Schedule("Balanced", "Standard", districtId, -5);

        Assert.True(state.IsScheduled);
        Assert.Equal("Balanced", state.Focus);
        Assert.Equal("Standard", state.Intensity);
        Assert.Equal(districtId, state.FocusDistrictId);
        Assert.Equal(0, state.ReadyAtTickId);
        Assert.True(state.IsReady(0));

        state.Clear();

        Assert.False(state.IsScheduled);
        Assert.Equal(string.Empty, state.Focus);
        Assert.Null(state.FocusDistrictId);
        Assert.Equal(0, state.ReadyAtTickId);
    }
}
