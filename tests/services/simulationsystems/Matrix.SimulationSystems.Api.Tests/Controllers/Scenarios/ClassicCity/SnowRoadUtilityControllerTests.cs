using Matrix.SimulationSystems.Api.Controllers.Scenarios.ClassicCity;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.DispatchCityRoadAccessMaintenance;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadAccessStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.SetCityRoadAccessEmergencyMode;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.Common;
using
    Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.DispatchCitySnowRemovalMaintenance;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.GetCitySnowRemovalStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.SetCitySnowRemovalEmergencyMode;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.
    DispatchCityUtilityIncidentResponse;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.
    GetCityDistrictUtilityIncidentConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.GetCityUtilityIncidentStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.
    SetCityUtilityIncidentEmergencyMode;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.RoadAccess.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.RoadAccess.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.SnowRemoval.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.SnowRemoval.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Views;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.SimulationSystems.Api.Tests.TestSupport.SimulationSystemsApiTestSupport;

namespace Matrix.SimulationSystems.Api.Tests.Controllers.Scenarios.ClassicCity
{
    public sealed class SnowRoadUtilityControllerTests
    {
        [Fact]
        public async Task SnowRemovalEndpoints_MapStatusAndDispatch()
        {
            var cityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
            var sender = new FakeSender();
            sender.Handle<GetCitySnowRemovalStatusQuery, CitySnowRemovalStatusDto?>(_
                => CreateSnowRemovalStatusDto(cityId));
            sender.Handle<SetCitySnowRemovalEmergencyModeCommand, CitySnowRemovalStatusDto?>(command =>
            {
                Assert.True(command.Enabled);
                return CreateSnowRemovalStatusDto(
                    cityId: cityId,
                    emergencyModeEnabled: true);
            });
            sender.Handle<DispatchCitySnowRemovalMaintenanceCommand, CitySnowRemovalStatusDto?>(command =>
            {
                Assert.Equal(
                    expected: "Routes",
                    actual: command.Focus);
                Assert.Equal(
                    expected: "Focused",
                    actual: command.Intensity);
                return CreateSnowRemovalStatusDto(cityId);
            });
            var controller = new SnowRemovalController(sender);

            IResult get = await controller.Get(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult set = await controller.SetEmergencyMode(
                cityId: cityId,
                request: new SetCitySnowRemovalEmergencyModeRequest(true),
                cancellationToken: CancellationToken.None);
            IResult dispatch = await controller.DispatchMaintenance(
                cityId: cityId,
                request: new DispatchCitySnowRemovalMaintenanceRequest(
                    Focus: "Routes",
                    Intensity: "Focused",
                    EmergencyOverride: false),
                cancellationToken: CancellationToken.None);

            CitySnowRemovalStatusView getView = AssertResult<CitySnowRemovalStatusView>(
                result: get,
                expectedStatusCode: StatusCodes.Status200OK);
            CitySnowRemovalStatusView setView = AssertResult<CitySnowRemovalStatusView>(
                result: set,
                expectedStatusCode: StatusCodes.Status200OK);
            CitySnowRemovalStatusView dispatchView = AssertResult<CitySnowRemovalStatusView>(
                result: dispatch,
                expectedStatusCode: StatusCodes.Status200OK);

            Assert.Equal(
                expected: cityId,
                actual: getView.CityId);
            Assert.True(setView.EmergencyModeEnabled);
            Assert.Equal(
                expected: "Focused",
                actual: dispatchView.AppliedIntensity);
        }

        [Fact]
        public async Task RoadAccessEndpoints_MapSegmentsAndDispatch()
        {
            var cityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
            var sender = new FakeSender();
            sender.Handle<GetCityRoadAccessStatusQuery, CityRoadAccessStatusDto?>(_
                => CreateRoadAccessStatusDto(cityId));
            sender.Handle<GetCityRoadSegmentConditionsQuery, CityRoadSegmentConditionsDto?>(_
                => CreateRoadSegmentConditionsDto(cityId));
            sender.Handle<SetCityRoadAccessEmergencyModeCommand, CityRoadAccessStatusDto?>(command =>
            {
                Assert.True(command.Enabled);
                return CreateRoadAccessStatusDto(
                    cityId: cityId,
                    emergencyModeEnabled: true);
            });
            sender.Handle<DispatchCityRoadAccessMaintenanceCommand, CityRoadAccessStatusDto?>(command =>
            {
                Assert.Equal(
                    expected: "Corridors",
                    actual: command.Focus);
                Assert.Equal(
                    expected: "Stabilize",
                    actual: command.Intensity);
                return CreateRoadAccessStatusDto(cityId);
            });
            var controller = new RoadAccessController(sender);

            IResult get = await controller.Get(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult segments = await controller.GetSegments(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult set = await controller.SetEmergencyMode(
                cityId: cityId,
                request: new SetCityRoadAccessEmergencyModeRequest(true),
                cancellationToken: CancellationToken.None);
            IResult dispatch = await controller.DispatchMaintenance(
                cityId: cityId,
                request: new DispatchCityRoadAccessMaintenanceRequest(
                    Focus: "Corridors",
                    Intensity: "Stabilize",
                    EmergencyOverride: false),
                cancellationToken: CancellationToken.None);

            CityRoadAccessStatusView getView = AssertResult<CityRoadAccessStatusView>(
                result: get,
                expectedStatusCode: StatusCodes.Status200OK);
            CityRoadSegmentConditionsView segmentView = AssertResult<CityRoadSegmentConditionsView>(
                result: segments,
                expectedStatusCode: StatusCodes.Status200OK);
            CityRoadAccessStatusView setView = AssertResult<CityRoadAccessStatusView>(
                result: set,
                expectedStatusCode: StatusCodes.Status200OK);
            CityRoadAccessStatusView dispatchView = AssertResult<CityRoadAccessStatusView>(
                result: dispatch,
                expectedStatusCode: StatusCodes.Status200OK);

            Assert.Equal(
                expected: cityId,
                actual: getView.CityId);
            Assert.Single(segmentView.Segments);
            Assert.Equal(
                expected: "Central Connector",
                actual: segmentView.Segments[0].Name);
            Assert.True(setView.EmergencyModeEnabled);
            Assert.Equal(
                expected: "Stabilize",
                actual: dispatchView.AppliedIntensity);
        }

        [Fact]
        public async Task UtilityIncidentEndpoints_MapDistrictsFocusAndConflict()
        {
            var cityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
            var districtId = Guid.Parse("b6130689-a065-4cf3-902a-d26a96756493");
            var sender = new FakeSender();
            sender.Handle<GetCityUtilityIncidentStatusQuery, CityUtilityIncidentStatusDto?>(_
                => CreateUtilityIncidentStatusDto(
                    cityId: cityId,
                    focusDistrictId: districtId));
            sender.Handle<GetCityDistrictUtilityIncidentConditionsQuery, CityDistrictUtilityIncidentConditionsDto?>(_
                => CreateUtilityIncidentDistrictConditionsDto(cityId));
            sender.Handle<SetCityUtilityIncidentEmergencyModeCommand, CityUtilityIncidentStatusDto?>(command =>
            {
                Assert.True(command.Enabled);
                return CreateUtilityIncidentStatusDto(
                    cityId: cityId,
                    emergencyModeEnabled: true,
                    focusDistrictId: districtId);
            });
            sender.Handle<DispatchCityUtilityIncidentResponseCommand, CityUtilityIncidentStatusDto?>(command =>
            {
                Assert.Equal(
                    expected: "Restoration",
                    actual: command.Focus);
                Assert.Equal(
                    expected: "Rapid",
                    actual: command.Intensity);
                Assert.True(command.EmergencyOverride);
                Assert.Equal(
                    expected: districtId,
                    actual: command.FocusDistrictId);
                return CreateUtilityIncidentStatusDto(
                    cityId: cityId,
                    budgetAuthorizationStatus: "Denied",
                    focusDistrictId: districtId);
            });
            var controller = new UtilityIncidentsController(sender);

            IResult get = await controller.Get(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult districts = await controller.GetDistricts(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult set = await controller.SetEmergencyMode(
                cityId: cityId,
                request: new SetCityUtilityIncidentEmergencyModeRequest(true),
                cancellationToken: CancellationToken.None);
            IResult dispatch = await controller.DispatchResponse(
                cityId: cityId,
                request: new DispatchCityUtilityIncidentResponseRequest(
                    Focus: "Restoration",
                    Intensity: "Rapid",
                    DistrictId: districtId,
                    EmergencyOverride: true),
                cancellationToken: CancellationToken.None);

            CityUtilityIncidentStatusView getView = AssertResult<CityUtilityIncidentStatusView>(
                result: get,
                expectedStatusCode: StatusCodes.Status200OK);
            CityDistrictUtilityIncidentConditionsView districtView =
                AssertResult<CityDistrictUtilityIncidentConditionsView>(
                    result: districts,
                    expectedStatusCode: StatusCodes.Status200OK);
            CityUtilityIncidentStatusView setView = AssertResult<CityUtilityIncidentStatusView>(
                result: set,
                expectedStatusCode: StatusCodes.Status200OK);
            CityUtilityIncidentStatusView dispatchView = AssertResult<CityUtilityIncidentStatusView>(
                result: dispatch,
                expectedStatusCode: StatusCodes.Status409Conflict);

            Assert.Equal(
                expected: districtId,
                actual: getView.FocusDistrictId);
            Assert.Single(districtView.Districts);
            Assert.Equal(
                expected: 0.61m,
                actual: districtView.Districts[0].RestorationPriorityIndex);
            Assert.True(setView.EmergencyModeEnabled);
            Assert.Equal(
                expected: "Denied",
                actual: dispatchView.BudgetAuthorizationStatus);
        }
    }
}
