using Matrix.SimulationSystems.Api.Controllers.Scenarios.ClassicCity;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.DispatchCityDrainageMaintenance;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.GetCityDrainageStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.SetCityDrainageEmergencyMode;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.DispatchCityHeatingMaintenance;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.GetCityDistrictHeatingConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.GetCityHeatingStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.SetCityHeatingEmergencyMode;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Drainage.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Drainage.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Views;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.SimulationSystems.Api.Tests.TestSupport.SimulationSystemsApiTestSupport;

namespace Matrix.SimulationSystems.Api.Tests.Controllers.Scenarios.ClassicCity
{
    public sealed class DrainageAndHeatingControllerTests
    {
        [Fact]
        public async Task DrainageEndpoints_MapViewsAndConflictStatus()
        {
            var cityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
            var sender = new FakeSender();
            sender.Handle<GetCityDrainageStatusQuery, CityDrainageStatusDto?>(query =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: query.CityId);
                return CreateDrainageStatusDto(cityId);
            });
            sender.Handle<SetCityDrainageEmergencyModeCommand, CityDrainageStatusDto?>(command =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: command.CityId);
                Assert.True(command.Enabled);
                return CreateDrainageStatusDto(
                    cityId: cityId,
                    emergencyModeEnabled: true);
            });
            sender.Handle<DispatchCityDrainageMaintenanceCommand, CityDrainageStatusDto?>(command =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: command.CityId);
                Assert.Equal(
                    expected: "Pumps",
                    actual: command.Focus);
                Assert.Equal(
                    expected: "Elevated",
                    actual: command.Intensity);
                Assert.True(command.EmergencyOverride);
                return CreateDrainageStatusDto(
                    cityId: cityId,
                    budgetAuthorizationStatus: "Denied");
            });
            var controller = new DrainageController(sender);

            IResult get = await controller.Get(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult set = await controller.SetEmergencyMode(
                cityId: cityId,
                request: new SetCityDrainageEmergencyModeRequest(true),
                cancellationToken: CancellationToken.None);
            IResult dispatch = await controller.DispatchMaintenance(
                cityId: cityId,
                request: new DispatchCityDrainageMaintenanceRequest(
                    Focus: "Pumps",
                    Intensity: "Elevated",
                    EmergencyOverride: true),
                cancellationToken: CancellationToken.None);

            CityDrainageStatusView getView = AssertResult<CityDrainageStatusView>(
                result: get,
                expectedStatusCode: StatusCodes.Status200OK);
            CityDrainageStatusView setView = AssertResult<CityDrainageStatusView>(
                result: set,
                expectedStatusCode: StatusCodes.Status200OK);
            CityDrainageStatusView dispatchView = AssertResult<CityDrainageStatusView>(
                result: dispatch,
                expectedStatusCode: StatusCodes.Status409Conflict);

            Assert.Equal(
                expected: cityId,
                actual: getView.CityId);
            Assert.True(setView.EmergencyModeEnabled);
            Assert.Equal(
                expected: "Denied",
                actual: dispatchView.BudgetAuthorizationStatus);
            Assert.Equal(
                expected: "Balanced",
                actual: dispatchView.PendingOperation!.Focus);
        }

        [Fact]
        public async Task HeatingEndpoints_MapStatusDistrictsAndDispatch()
        {
            var cityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
            var sender = new FakeSender();
            sender.Handle<GetCityHeatingStatusQuery, CityHeatingStatusDto?>(query =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: query.CityId);
                return CreateHeatingStatusDto(cityId);
            });
            sender.Handle<GetCityDistrictHeatingConditionsQuery, CityDistrictHeatingConditionsDto?>(query =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: query.CityId);
                return CreateHeatingDistrictConditionsDto(cityId);
            });
            sender.Handle<SetCityHeatingEmergencyModeCommand, CityHeatingStatusDto?>(command =>
            {
                Assert.True(command.Enabled);
                return CreateHeatingStatusDto(
                    cityId: cityId,
                    emergencyModeEnabled: true);
            });
            sender.Handle<DispatchCityHeatingMaintenanceCommand, CityHeatingStatusDto?>(command =>
            {
                Assert.Equal(
                    expected: "Boilers",
                    actual: command.Focus);
                Assert.Equal(
                    expected: "Focused",
                    actual: command.Intensity);
                Assert.False(command.EmergencyOverride);
                return CreateHeatingStatusDto(cityId);
            });
            var controller = new HeatingController(sender);

            IResult get = await controller.Get(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult districts = await controller.GetDistricts(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult set = await controller.SetEmergencyMode(
                cityId: cityId,
                request: new SetCityHeatingEmergencyModeRequest(true),
                cancellationToken: CancellationToken.None);
            IResult dispatch = await controller.DispatchMaintenance(
                cityId: cityId,
                request: new DispatchCityHeatingMaintenanceRequest(
                    Focus: "Boilers",
                    Intensity: "Focused",
                    EmergencyOverride: false),
                cancellationToken: CancellationToken.None);

            CityHeatingStatusView getView = AssertResult<CityHeatingStatusView>(
                result: get,
                expectedStatusCode: StatusCodes.Status200OK);
            CityDistrictHeatingConditionsView districtView =
                AssertResult<CityDistrictHeatingConditionsView>(
                    result: districts,
                    expectedStatusCode: StatusCodes.Status200OK);
            CityHeatingStatusView setView = AssertResult<CityHeatingStatusView>(
                result: set,
                expectedStatusCode: StatusCodes.Status200OK);
            CityHeatingStatusView dispatchView = AssertResult<CityHeatingStatusView>(
                result: dispatch,
                expectedStatusCode: StatusCodes.Status200OK);

            Assert.Equal(
                expected: cityId,
                actual: getView.CityId);
            Assert.Single(districtView.Districts);
            Assert.Equal(
                expected: 0.57m,
                actual: districtView.Districts[0].MaintenancePriorityIndex);
            Assert.True(setView.EmergencyModeEnabled);
            Assert.Equal(
                expected: "Focused",
                actual: dispatchView.AppliedIntensity);
        }
    }
}
