using Matrix.SimulationSystems.Api.Controllers.Scenarios.ClassicCity;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.DispatchCityRoadAccessMaintenance;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadAccessStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.SetCityRoadAccessEmergencyMode;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.DispatchCitySnowRemovalMaintenance;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.GetCitySnowRemovalStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.SetCitySnowRemovalEmergencyMode;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.DispatchCityUtilityIncidentResponse;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.GetCityDistrictUtilityIncidentConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.GetCityUtilityIncidentStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.SetCityUtilityIncidentEmergencyMode;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.RoadAccess.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.RoadAccess.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.SnowRemoval.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.SnowRemoval.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Views;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.SimulationSystems.Api.Tests.TestSupport.SimulationSystemsApiTestSupport;

namespace Matrix.SimulationSystems.Api.Tests.Controllers.Scenarios.ClassicCity;

public sealed class SnowRoadUtilityControllerTests
{
    [Fact]
    public async Task SnowRemovalEndpoints_MapStatusAndDispatch()
    {
        Guid cityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
        var sender = new FakeSender();
        sender.Handle<GetCitySnowRemovalStatusQuery, CitySnowRemovalStatusDto?>(_ => CreateSnowRemovalStatusDto(cityId));
        sender.Handle<SetCitySnowRemovalEmergencyModeCommand, CitySnowRemovalStatusDto?>(command =>
        {
            Assert.True(command.Enabled);
            return CreateSnowRemovalStatusDto(cityId, emergencyModeEnabled: true);
        });
        sender.Handle<DispatchCitySnowRemovalMaintenanceCommand, CitySnowRemovalStatusDto?>(command =>
        {
            Assert.Equal("Routes", command.Focus);
            Assert.Equal("Focused", command.Intensity);
            return CreateSnowRemovalStatusDto(cityId);
        });
        var controller = new SnowRemovalController(sender);

        IResult get = await controller.Get(cityId, CancellationToken.None);
        IResult set = await controller.SetEmergencyMode(cityId, new SetCitySnowRemovalEmergencyModeRequest(true), CancellationToken.None);
        IResult dispatch = await controller.DispatchMaintenance(cityId, new DispatchCitySnowRemovalMaintenanceRequest("Routes", "Focused", false), CancellationToken.None);

        CitySnowRemovalStatusView getView = AssertResult<CitySnowRemovalStatusView>(get, StatusCodes.Status200OK);
        CitySnowRemovalStatusView setView = AssertResult<CitySnowRemovalStatusView>(set, StatusCodes.Status200OK);
        CitySnowRemovalStatusView dispatchView = AssertResult<CitySnowRemovalStatusView>(dispatch, StatusCodes.Status200OK);

        Assert.Equal(cityId, getView.CityId);
        Assert.True(setView.EmergencyModeEnabled);
        Assert.Equal("Focused", dispatchView.AppliedIntensity);
    }

    [Fact]
    public async Task RoadAccessEndpoints_MapSegmentsAndDispatch()
    {
        Guid cityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
        var sender = new FakeSender();
        sender.Handle<GetCityRoadAccessStatusQuery, CityRoadAccessStatusDto?>(_ => CreateRoadAccessStatusDto(cityId));
        sender.Handle<GetCityRoadSegmentConditionsQuery, CityRoadSegmentConditionsDto?>(_ => CreateRoadSegmentConditionsDto(cityId));
        sender.Handle<SetCityRoadAccessEmergencyModeCommand, CityRoadAccessStatusDto?>(command =>
        {
            Assert.True(command.Enabled);
            return CreateRoadAccessStatusDto(cityId, emergencyModeEnabled: true);
        });
        sender.Handle<DispatchCityRoadAccessMaintenanceCommand, CityRoadAccessStatusDto?>(command =>
        {
            Assert.Equal("Corridors", command.Focus);
            Assert.Equal("Stabilize", command.Intensity);
            return CreateRoadAccessStatusDto(cityId);
        });
        var controller = new RoadAccessController(sender);

        IResult get = await controller.Get(cityId, CancellationToken.None);
        IResult segments = await controller.GetSegments(cityId, CancellationToken.None);
        IResult set = await controller.SetEmergencyMode(cityId, new SetCityRoadAccessEmergencyModeRequest(true), CancellationToken.None);
        IResult dispatch = await controller.DispatchMaintenance(cityId, new DispatchCityRoadAccessMaintenanceRequest("Corridors", "Stabilize", false), CancellationToken.None);

        CityRoadAccessStatusView getView = AssertResult<CityRoadAccessStatusView>(get, StatusCodes.Status200OK);
        CityRoadSegmentConditionsView segmentView = AssertResult<CityRoadSegmentConditionsView>(segments, StatusCodes.Status200OK);
        CityRoadAccessStatusView setView = AssertResult<CityRoadAccessStatusView>(set, StatusCodes.Status200OK);
        CityRoadAccessStatusView dispatchView = AssertResult<CityRoadAccessStatusView>(dispatch, StatusCodes.Status200OK);

        Assert.Equal(cityId, getView.CityId);
        Assert.Single(segmentView.Segments);
        Assert.Equal("Central Connector", segmentView.Segments[0].Name);
        Assert.True(setView.EmergencyModeEnabled);
        Assert.Equal("Stabilize", dispatchView.AppliedIntensity);
    }

    [Fact]
    public async Task UtilityIncidentEndpoints_MapDistrictsFocusAndConflict()
    {
        Guid cityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
        Guid districtId = Guid.Parse("b6130689-a065-4cf3-902a-d26a96756493");
        var sender = new FakeSender();
        sender.Handle<GetCityUtilityIncidentStatusQuery, CityUtilityIncidentStatusDto?>(_ => CreateUtilityIncidentStatusDto(cityId, focusDistrictId: districtId));
        sender.Handle<GetCityDistrictUtilityIncidentConditionsQuery, CityDistrictUtilityIncidentConditionsDto?>(_ => CreateUtilityIncidentDistrictConditionsDto(cityId));
        sender.Handle<SetCityUtilityIncidentEmergencyModeCommand, CityUtilityIncidentStatusDto?>(command =>
        {
            Assert.True(command.Enabled);
            return CreateUtilityIncidentStatusDto(cityId, emergencyModeEnabled: true, focusDistrictId: districtId);
        });
        sender.Handle<DispatchCityUtilityIncidentResponseCommand, CityUtilityIncidentStatusDto?>(command =>
        {
            Assert.Equal("Restoration", command.Focus);
            Assert.Equal("Rapid", command.Intensity);
            Assert.True(command.EmergencyOverride);
            Assert.Equal(districtId, command.FocusDistrictId);
            return CreateUtilityIncidentStatusDto(cityId, budgetAuthorizationStatus: "Denied", focusDistrictId: districtId);
        });
        var controller = new UtilityIncidentsController(sender);

        IResult get = await controller.Get(cityId, CancellationToken.None);
        IResult districts = await controller.GetDistricts(cityId, CancellationToken.None);
        IResult set = await controller.SetEmergencyMode(cityId, new SetCityUtilityIncidentEmergencyModeRequest(true), CancellationToken.None);
        IResult dispatch = await controller.DispatchResponse(
            cityId,
            new DispatchCityUtilityIncidentResponseRequest("Restoration", "Rapid", districtId, true),
            CancellationToken.None);

        CityUtilityIncidentStatusView getView = AssertResult<CityUtilityIncidentStatusView>(get, StatusCodes.Status200OK);
        CityDistrictUtilityIncidentConditionsView districtView =
            AssertResult<CityDistrictUtilityIncidentConditionsView>(districts, StatusCodes.Status200OK);
        CityUtilityIncidentStatusView setView = AssertResult<CityUtilityIncidentStatusView>(set, StatusCodes.Status200OK);
        CityUtilityIncidentStatusView dispatchView = AssertResult<CityUtilityIncidentStatusView>(dispatch, StatusCodes.Status409Conflict);

        Assert.Equal(districtId, getView.FocusDistrictId);
        Assert.Single(districtView.Districts);
        Assert.Equal(0.61m, districtView.Districts[0].RestorationPriorityIndex);
        Assert.True(setView.EmergencyModeEnabled);
        Assert.Equal("Denied", dispatchView.BudgetAuthorizationStatus);
    }
}
