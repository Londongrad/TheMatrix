using Matrix.SimulationSystems.Api.Controllers.Scenarios.ClassicCity;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.
    DispatchCityPowerDistributionMaintenance;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.
    GetCityDistrictPowerDistributionConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.
    GetCityPowerDistributionStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.
    SetCityPowerDistributionEmergencyMode;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.DispatchCitySanitationMaintenance;
using
    Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.GetCityDistrictSanitationConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.GetCitySanitationStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.SetCitySanitationEmergencyMode;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.Common;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.
    DispatchCityWaterDistributionMaintenance;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.
    GetCityDistrictWaterDistributionConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.
    GetCityWaterDistributionStatus;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.
    SetCityWaterDistributionEmergencyMode;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.WaterDistribution.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.WaterDistribution.Views;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.SimulationSystems.Api.Tests.TestSupport.SimulationSystemsApiTestSupport;

namespace Matrix.SimulationSystems.Api.Tests.Controllers.Scenarios.ClassicCity
{
    public sealed class UtilityControllersTests
    {
        [Fact]
        public async Task WaterDistributionEndpoints_MapStatusDistrictsAndDispatch()
        {
            var cityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
            var sender = new FakeSender();
            sender.Handle<GetCityWaterDistributionStatusQuery, CityWaterDistributionStatusDto?>(_
                => CreateWaterDistributionStatusDto(cityId));
            sender
               .Handle<GetCityDistrictWaterDistributionConditionsQuery, CityDistrictWaterDistributionConditionsDto?>(_
                    => CreateWaterDistrictConditionsDto(cityId));
            sender.Handle<SetCityWaterDistributionEmergencyModeCommand, CityWaterDistributionStatusDto?>(command =>
            {
                Assert.True(command.Enabled);
                return CreateWaterDistributionStatusDto(
                    cityId: cityId,
                    emergencyModeEnabled: true);
            });
            sender.Handle<DispatchCityWaterDistributionMaintenanceCommand, CityWaterDistributionStatusDto?>(command =>
            {
                Assert.Equal(
                    expected: "Treatment",
                    actual: command.Focus);
                Assert.Equal(
                    expected: "Elevated",
                    actual: command.Intensity);
                return CreateWaterDistributionStatusDto(cityId);
            });
            var controller = new WaterDistributionController(sender);

            IResult get = await controller.Get(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult districts = await controller.GetDistricts(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult set = await controller.SetEmergencyMode(
                cityId: cityId,
                request: new SetCityWaterDistributionEmergencyModeRequest(true),
                cancellationToken: CancellationToken.None);
            IResult dispatch = await controller.DispatchMaintenance(
                cityId: cityId,
                request: new DispatchCityWaterDistributionMaintenanceRequest(
                    Focus: "Treatment",
                    Intensity: "Elevated",
                    EmergencyOverride: false),
                cancellationToken: CancellationToken.None);

            CityWaterDistributionStatusView getView = AssertResult<CityWaterDistributionStatusView>(
                result: get,
                expectedStatusCode: StatusCodes.Status200OK);
            CityDistrictWaterDistributionConditionsView districtView =
                AssertResult<CityDistrictWaterDistributionConditionsView>(
                    result: districts,
                    expectedStatusCode: StatusCodes.Status200OK);
            CityWaterDistributionStatusView setView = AssertResult<CityWaterDistributionStatusView>(
                result: set,
                expectedStatusCode: StatusCodes.Status200OK);
            CityWaterDistributionStatusView dispatchView = AssertResult<CityWaterDistributionStatusView>(
                result: dispatch,
                expectedStatusCode: StatusCodes.Status200OK);

            Assert.Equal(
                expected: cityId,
                actual: getView.CityId);
            Assert.Single(districtView.Districts);
            Assert.Equal(
                expected: 0.16m,
                actual: districtView.Districts[0].QualityRiskIndex);
            Assert.True(setView.EmergencyModeEnabled);
            Assert.Equal(
                expected: "Elevated",
                actual: dispatchView.AppliedIntensity);
        }

        [Fact]
        public async Task SanitationEndpoints_MapStatusDistrictsAndDispatch()
        {
            var cityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
            var sender = new FakeSender();
            sender.Handle<GetCitySanitationStatusQuery, CitySanitationStatusDto?>(_
                => CreateSanitationStatusDto(cityId));
            sender.Handle<GetCityDistrictSanitationConditionsQuery, CityDistrictSanitationConditionsDto?>(_
                => CreateSanitationDistrictConditionsDto(cityId));
            sender.Handle<SetCitySanitationEmergencyModeCommand, CitySanitationStatusDto?>(command =>
            {
                Assert.True(command.Enabled);
                return CreateSanitationStatusDto(
                    cityId: cityId,
                    emergencyModeEnabled: true);
            });
            sender.Handle<DispatchCitySanitationMaintenanceCommand, CitySanitationStatusDto?>(command =>
            {
                Assert.Equal(
                    expected: "Overflow",
                    actual: command.Focus);
                Assert.Equal(
                    expected: "Stabilize",
                    actual: command.Intensity);
                return CreateSanitationStatusDto(cityId);
            });
            var controller = new SanitationController(sender);

            IResult get = await controller.Get(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult districts = await controller.GetDistricts(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult set = await controller.SetEmergencyMode(
                cityId: cityId,
                request: new SetCitySanitationEmergencyModeRequest(true),
                cancellationToken: CancellationToken.None);
            IResult dispatch = await controller.DispatchMaintenance(
                cityId: cityId,
                request: new DispatchCitySanitationMaintenanceRequest(
                    Focus: "Overflow",
                    Intensity: "Stabilize",
                    EmergencyOverride: false),
                cancellationToken: CancellationToken.None);

            CitySanitationStatusView getView = AssertResult<CitySanitationStatusView>(
                result: get,
                expectedStatusCode: StatusCodes.Status200OK);
            CityDistrictSanitationConditionsView districtView =
                AssertResult<CityDistrictSanitationConditionsView>(
                    result: districts,
                    expectedStatusCode: StatusCodes.Status200OK);
            CitySanitationStatusView setView = AssertResult<CitySanitationStatusView>(
                result: set,
                expectedStatusCode: StatusCodes.Status200OK);
            CitySanitationStatusView dispatchView = AssertResult<CitySanitationStatusView>(
                result: dispatch,
                expectedStatusCode: StatusCodes.Status200OK);

            Assert.Equal(
                expected: cityId,
                actual: getView.CityId);
            Assert.Single(districtView.Districts);
            Assert.Equal(
                expected: 0.19m,
                actual: districtView.Districts[0].OverflowRiskIndex);
            Assert.True(setView.EmergencyModeEnabled);
            Assert.Equal(
                expected: "Stabilize",
                actual: dispatchView.AppliedIntensity);
        }

        [Fact]
        public async Task PowerDistributionEndpoints_MapStatusDistrictsAndDispatch()
        {
            var cityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
            var sender = new FakeSender();
            sender.Handle<GetCityPowerDistributionStatusQuery, CityPowerDistributionStatusDto?>(_
                => CreatePowerDistributionStatusDto(cityId));
            sender
               .Handle<GetCityDistrictPowerDistributionConditionsQuery, CityDistrictPowerDistributionConditionsDto?>(_
                    => CreatePowerDistrictConditionsDto(cityId));
            sender.Handle<SetCityPowerDistributionEmergencyModeCommand, CityPowerDistributionStatusDto?>(command =>
            {
                Assert.True(command.Enabled);
                return CreatePowerDistributionStatusDto(
                    cityId: cityId,
                    emergencyModeEnabled: true);
            });
            sender.Handle<DispatchCityPowerDistributionMaintenanceCommand, CityPowerDistributionStatusDto?>(command =>
            {
                Assert.Equal(
                    expected: "Substations",
                    actual: command.Focus);
                Assert.Equal(
                    expected: "Elevated",
                    actual: command.Intensity);
                return CreatePowerDistributionStatusDto(cityId);
            });
            var controller = new PowerDistributionController(sender);

            IResult get = await controller.Get(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult districts = await controller.GetDistricts(
                cityId: cityId,
                cancellationToken: CancellationToken.None);
            IResult set = await controller.SetEmergencyMode(
                cityId: cityId,
                request: new SetCityPowerDistributionEmergencyModeRequest(true),
                cancellationToken: CancellationToken.None);
            IResult dispatch = await controller.DispatchMaintenance(
                cityId: cityId,
                request: new DispatchCityPowerDistributionMaintenanceRequest(
                    Focus: "Substations",
                    Intensity: "Elevated",
                    EmergencyOverride: false),
                cancellationToken: CancellationToken.None);

            CityPowerDistributionStatusView getView = AssertResult<CityPowerDistributionStatusView>(
                result: get,
                expectedStatusCode: StatusCodes.Status200OK);
            CityDistrictPowerDistributionConditionsView districtView =
                AssertResult<CityDistrictPowerDistributionConditionsView>(
                    result: districts,
                    expectedStatusCode: StatusCodes.Status200OK);
            CityPowerDistributionStatusView setView = AssertResult<CityPowerDistributionStatusView>(
                result: set,
                expectedStatusCode: StatusCodes.Status200OK);
            CityPowerDistributionStatusView dispatchView = AssertResult<CityPowerDistributionStatusView>(
                result: dispatch,
                expectedStatusCode: StatusCodes.Status200OK);

            Assert.Equal(
                expected: cityId,
                actual: getView.CityId);
            Assert.Single(districtView.Districts);
            Assert.Equal(
                expected: 0.16m,
                actual: districtView.Districts[0].RestorationStrainIndex);
            Assert.True(setView.EmergencyModeEnabled);
            Assert.Equal(
                expected: "Elevated",
                actual: dispatchView.AppliedIntensity);
        }
    }
}
