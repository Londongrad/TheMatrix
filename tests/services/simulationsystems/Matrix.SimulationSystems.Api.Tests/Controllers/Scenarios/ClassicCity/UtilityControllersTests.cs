using Matrix.SimulationSystems.Api.Controllers.Scenarios.ClassicCity;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.DispatchCityPowerDistributionMaintenance;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.GetCityDistrictPowerDistributionConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.GetCityPowerDistributionStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.SetCityPowerDistributionEmergencyMode;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.DispatchCitySanitationMaintenance;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.GetCityDistrictSanitationConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.GetCitySanitationStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.SetCitySanitationEmergencyMode;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.DispatchCityWaterDistributionMaintenance;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.GetCityDistrictWaterDistributionConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.GetCityWaterDistributionStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.SetCityWaterDistributionEmergencyMode;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.WaterDistribution.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.WaterDistribution.Views;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.SimulationSystems.Api.Tests.TestSupport.SimulationSystemsApiTestSupport;

namespace Matrix.SimulationSystems.Api.Tests.Controllers.Scenarios.ClassicCity;

public sealed class UtilityControllersTests
{
    [Fact]
    public async Task WaterDistributionEndpoints_MapStatusDistrictsAndDispatch()
    {
        Guid cityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
        var sender = new FakeSender();
        sender.Handle<GetCityWaterDistributionStatusQuery, CityWaterDistributionStatusDto?>(_ => CreateWaterDistributionStatusDto(cityId));
        sender.Handle<GetCityDistrictWaterDistributionConditionsQuery, CityDistrictWaterDistributionConditionsDto?>(_ => CreateWaterDistrictConditionsDto(cityId));
        sender.Handle<SetCityWaterDistributionEmergencyModeCommand, CityWaterDistributionStatusDto?>(command =>
        {
            Assert.True(command.Enabled);
            return CreateWaterDistributionStatusDto(cityId, emergencyModeEnabled: true);
        });
        sender.Handle<DispatchCityWaterDistributionMaintenanceCommand, CityWaterDistributionStatusDto?>(command =>
        {
            Assert.Equal("Treatment", command.Focus);
            Assert.Equal("Elevated", command.Intensity);
            return CreateWaterDistributionStatusDto(cityId);
        });
        var controller = new WaterDistributionController(sender);

        IResult get = await controller.Get(cityId, CancellationToken.None);
        IResult districts = await controller.GetDistricts(cityId, CancellationToken.None);
        IResult set = await controller.SetEmergencyMode(cityId, new SetCityWaterDistributionEmergencyModeRequest(true), CancellationToken.None);
        IResult dispatch = await controller.DispatchMaintenance(cityId, new DispatchCityWaterDistributionMaintenanceRequest("Treatment", "Elevated", false), CancellationToken.None);

        CityWaterDistributionStatusView getView = AssertResult<CityWaterDistributionStatusView>(get, StatusCodes.Status200OK);
        CityDistrictWaterDistributionConditionsView districtView =
            AssertResult<CityDistrictWaterDistributionConditionsView>(districts, StatusCodes.Status200OK);
        CityWaterDistributionStatusView setView = AssertResult<CityWaterDistributionStatusView>(set, StatusCodes.Status200OK);
        CityWaterDistributionStatusView dispatchView = AssertResult<CityWaterDistributionStatusView>(dispatch, StatusCodes.Status200OK);

        Assert.Equal(cityId, getView.CityId);
        Assert.Single(districtView.Districts);
        Assert.Equal(0.16m, districtView.Districts[0].QualityRiskIndex);
        Assert.True(setView.EmergencyModeEnabled);
        Assert.Equal("Elevated", dispatchView.AppliedIntensity);
    }

    [Fact]
    public async Task SanitationEndpoints_MapStatusDistrictsAndDispatch()
    {
        Guid cityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
        var sender = new FakeSender();
        sender.Handle<GetCitySanitationStatusQuery, CitySanitationStatusDto?>(_ => CreateSanitationStatusDto(cityId));
        sender.Handle<GetCityDistrictSanitationConditionsQuery, CityDistrictSanitationConditionsDto?>(_ => CreateSanitationDistrictConditionsDto(cityId));
        sender.Handle<SetCitySanitationEmergencyModeCommand, CitySanitationStatusDto?>(command =>
        {
            Assert.True(command.Enabled);
            return CreateSanitationStatusDto(cityId, emergencyModeEnabled: true);
        });
        sender.Handle<DispatchCitySanitationMaintenanceCommand, CitySanitationStatusDto?>(command =>
        {
            Assert.Equal("Overflow", command.Focus);
            Assert.Equal("Stabilize", command.Intensity);
            return CreateSanitationStatusDto(cityId);
        });
        var controller = new SanitationController(sender);

        IResult get = await controller.Get(cityId, CancellationToken.None);
        IResult districts = await controller.GetDistricts(cityId, CancellationToken.None);
        IResult set = await controller.SetEmergencyMode(cityId, new SetCitySanitationEmergencyModeRequest(true), CancellationToken.None);
        IResult dispatch = await controller.DispatchMaintenance(cityId, new DispatchCitySanitationMaintenanceRequest("Overflow", "Stabilize", false), CancellationToken.None);

        CitySanitationStatusView getView = AssertResult<CitySanitationStatusView>(get, StatusCodes.Status200OK);
        CityDistrictSanitationConditionsView districtView =
            AssertResult<CityDistrictSanitationConditionsView>(districts, StatusCodes.Status200OK);
        CitySanitationStatusView setView = AssertResult<CitySanitationStatusView>(set, StatusCodes.Status200OK);
        CitySanitationStatusView dispatchView = AssertResult<CitySanitationStatusView>(dispatch, StatusCodes.Status200OK);

        Assert.Equal(cityId, getView.CityId);
        Assert.Single(districtView.Districts);
        Assert.Equal(0.19m, districtView.Districts[0].OverflowRiskIndex);
        Assert.True(setView.EmergencyModeEnabled);
        Assert.Equal("Stabilize", dispatchView.AppliedIntensity);
    }

    [Fact]
    public async Task PowerDistributionEndpoints_MapStatusDistrictsAndDispatch()
    {
        Guid cityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
        var sender = new FakeSender();
        sender.Handle<GetCityPowerDistributionStatusQuery, CityPowerDistributionStatusDto?>(_ => CreatePowerDistributionStatusDto(cityId));
        sender.Handle<GetCityDistrictPowerDistributionConditionsQuery, CityDistrictPowerDistributionConditionsDto?>(_ => CreatePowerDistrictConditionsDto(cityId));
        sender.Handle<SetCityPowerDistributionEmergencyModeCommand, CityPowerDistributionStatusDto?>(command =>
        {
            Assert.True(command.Enabled);
            return CreatePowerDistributionStatusDto(cityId, emergencyModeEnabled: true);
        });
        sender.Handle<DispatchCityPowerDistributionMaintenanceCommand, CityPowerDistributionStatusDto?>(command =>
        {
            Assert.Equal("Substations", command.Focus);
            Assert.Equal("Elevated", command.Intensity);
            return CreatePowerDistributionStatusDto(cityId);
        });
        var controller = new PowerDistributionController(sender);

        IResult get = await controller.Get(cityId, CancellationToken.None);
        IResult districts = await controller.GetDistricts(cityId, CancellationToken.None);
        IResult set = await controller.SetEmergencyMode(cityId, new SetCityPowerDistributionEmergencyModeRequest(true), CancellationToken.None);
        IResult dispatch = await controller.DispatchMaintenance(cityId, new DispatchCityPowerDistributionMaintenanceRequest("Substations", "Elevated", false), CancellationToken.None);

        CityPowerDistributionStatusView getView = AssertResult<CityPowerDistributionStatusView>(get, StatusCodes.Status200OK);
        CityDistrictPowerDistributionConditionsView districtView =
            AssertResult<CityDistrictPowerDistributionConditionsView>(districts, StatusCodes.Status200OK);
        CityPowerDistributionStatusView setView = AssertResult<CityPowerDistributionStatusView>(set, StatusCodes.Status200OK);
        CityPowerDistributionStatusView dispatchView = AssertResult<CityPowerDistributionStatusView>(dispatch, StatusCodes.Status200OK);

        Assert.Equal(cityId, getView.CityId);
        Assert.Single(districtView.Districts);
        Assert.Equal(0.16m, districtView.Districts[0].RestorationStrainIndex);
        Assert.True(setView.EmergencyModeEnabled);
        Assert.Equal("Elevated", dispatchView.AppliedIntensity);
    }
}
