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

namespace Matrix.SimulationSystems.Api.Tests.Controllers.Scenarios.ClassicCity;

public sealed class DrainageAndHeatingControllerTests
{
    [Fact]
    public async Task DrainageEndpoints_MapViewsAndConflictStatus()
    {
        Guid cityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
        var sender = new FakeSender();
        sender.Handle<GetCityDrainageStatusQuery, CityDrainageStatusDto?>(query =>
        {
            Assert.Equal(cityId, query.CityId);
            return CreateDrainageStatusDto(cityId);
        });
        sender.Handle<SetCityDrainageEmergencyModeCommand, CityDrainageStatusDto?>(command =>
        {
            Assert.Equal(cityId, command.CityId);
            Assert.True(command.Enabled);
            return CreateDrainageStatusDto(cityId, emergencyModeEnabled: true);
        });
        sender.Handle<DispatchCityDrainageMaintenanceCommand, CityDrainageStatusDto?>(command =>
        {
            Assert.Equal(cityId, command.CityId);
            Assert.Equal("Pumps", command.Focus);
            Assert.Equal("Elevated", command.Intensity);
            Assert.True(command.EmergencyOverride);
            return CreateDrainageStatusDto(cityId, budgetAuthorizationStatus: "Denied");
        });
        var controller = new DrainageController(sender);

        IResult get = await controller.Get(cityId, CancellationToken.None);
        IResult set = await controller.SetEmergencyMode(
            cityId,
            new SetCityDrainageEmergencyModeRequest(true),
            CancellationToken.None);
        IResult dispatch = await controller.DispatchMaintenance(
            cityId,
            new DispatchCityDrainageMaintenanceRequest("Pumps", "Elevated", true),
            CancellationToken.None);

        CityDrainageStatusView getView = AssertResult<CityDrainageStatusView>(get, StatusCodes.Status200OK);
        CityDrainageStatusView setView = AssertResult<CityDrainageStatusView>(set, StatusCodes.Status200OK);
        CityDrainageStatusView dispatchView = AssertResult<CityDrainageStatusView>(dispatch, StatusCodes.Status409Conflict);

        Assert.Equal(cityId, getView.CityId);
        Assert.True(setView.EmergencyModeEnabled);
        Assert.Equal("Denied", dispatchView.BudgetAuthorizationStatus);
        Assert.Equal("Balanced", dispatchView.PendingOperation!.Focus);
    }

    [Fact]
    public async Task HeatingEndpoints_MapStatusDistrictsAndDispatch()
    {
        Guid cityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
        var sender = new FakeSender();
        sender.Handle<GetCityHeatingStatusQuery, CityHeatingStatusDto?>(query =>
        {
            Assert.Equal(cityId, query.CityId);
            return CreateHeatingStatusDto(cityId);
        });
        sender.Handle<GetCityDistrictHeatingConditionsQuery, CityDistrictHeatingConditionsDto?>(query =>
        {
            Assert.Equal(cityId, query.CityId);
            return CreateHeatingDistrictConditionsDto(cityId);
        });
        sender.Handle<SetCityHeatingEmergencyModeCommand, CityHeatingStatusDto?>(command =>
        {
            Assert.True(command.Enabled);
            return CreateHeatingStatusDto(cityId, emergencyModeEnabled: true);
        });
        sender.Handle<DispatchCityHeatingMaintenanceCommand, CityHeatingStatusDto?>(command =>
        {
            Assert.Equal("Boilers", command.Focus);
            Assert.Equal("Focused", command.Intensity);
            Assert.False(command.EmergencyOverride);
            return CreateHeatingStatusDto(cityId);
        });
        var controller = new HeatingController(sender);

        IResult get = await controller.Get(cityId, CancellationToken.None);
        IResult districts = await controller.GetDistricts(cityId, CancellationToken.None);
        IResult set = await controller.SetEmergencyMode(
            cityId,
            new SetCityHeatingEmergencyModeRequest(true),
            CancellationToken.None);
        IResult dispatch = await controller.DispatchMaintenance(
            cityId,
            new DispatchCityHeatingMaintenanceRequest("Boilers", "Focused", false),
            CancellationToken.None);

        CityHeatingStatusView getView = AssertResult<CityHeatingStatusView>(get, StatusCodes.Status200OK);
        CityDistrictHeatingConditionsView districtView =
            AssertResult<CityDistrictHeatingConditionsView>(districts, StatusCodes.Status200OK);
        CityHeatingStatusView setView = AssertResult<CityHeatingStatusView>(set, StatusCodes.Status200OK);
        CityHeatingStatusView dispatchView = AssertResult<CityHeatingStatusView>(dispatch, StatusCodes.Status200OK);

        Assert.Equal(cityId, getView.CityId);
        Assert.Single(districtView.Districts);
        Assert.Equal(0.57m, districtView.Districts[0].MaintenancePriorityIndex);
        Assert.True(setView.EmergencyModeEnabled);
        Assert.Equal("Focused", dispatchView.AppliedIntensity);
    }
}
