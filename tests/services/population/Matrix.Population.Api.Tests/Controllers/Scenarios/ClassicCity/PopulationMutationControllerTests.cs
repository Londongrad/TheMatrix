using Matrix.Population.Api.Controllers.Scenarios.ClassicCity;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.RegisterDivorce;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.RegisterMarriage;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.FireResident;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.HireResident;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.RetireResident;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityEnvironment;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.Population.Api.Tests.TestSupport.PopulationApiTestSupport;

namespace Matrix.Population.Api.Tests.Controllers.Scenarios.ClassicCity
{
    public sealed class PopulationMutationControllerTests
    {
        [Fact]
        public async Task InitializeCityPopulation_MapsNestedRequest()
        {
            var cityId = Guid.Parse("d22b7a64-245d-4d96-8fb7-e4a0130686a4");
            var anchorId = Guid.Parse("4d679724-c724-4b9b-b402-cc7e303d42d8");
            var buildingId = Guid.Parse("a63f03d6-3320-4a95-8bb3-859f50f939aa");
            var sender = new FakeSender();
            sender.Handle<InitializeCityPopulationCommand, CityPopulationBootstrapSummaryDto>(command =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: command.CityId);
                Assert.Equal(
                    expected: new DateOnly(
                        year: 2048,
                        month: 6,
                        day: 1),
                    actual: command.CurrentDate);
                Assert.Equal(
                    expected: 60,
                    actual: command.PeopleCount);
                Assert.Equal(
                    expected: 77,
                    actual: command.RandomSeed);
                Assert.Equal(
                    expected: "Continental",
                    actual: command.Environment.ClimateZone);
                Assert.Equal(
                    expected: "Northern",
                    actual: command.Environment.Hemisphere);
                Assert.Equal(
                    expected: 180,
                    actual: command.Environment.UtcOffsetMinutes);
                Assert.Equal(
                    expected: 70,
                    actual: command.Tuning.HousingPressurePercent);
                Assert.Equal(
                    expected: 65,
                    actual: command.Tuning.EconomicStabilityPercent);
                Assert.Equal(
                    expected: 40,
                    actual: command.Tuning.SocialVolatilityPercent);
                Assert.Equal(
                    expected: 55,
                    actual: command.Tuning.FamilyFormationPercent);

                CityAnchorSeedItem anchor = Assert.Single(command.CityAnchors);
                Assert.Equal(
                    expected: anchorId,
                    actual: anchor.CityAnchorId);
                Assert.Equal(
                    expected: "Hospital",
                    actual: anchor.Name);

                ResidentialBuildingSeedItem building = Assert.Single(command.ResidentialBuildings);
                Assert.Equal(
                    expected: buildingId,
                    actual: building.ResidentialBuildingId);
                Assert.Equal(
                    expected: 120,
                    actual: building.ResidentCapacity);

                return CreateBootstrapSummaryDto(cityId);
            });
            var controller = new ClassicCityPopulationBootstrapController(sender);

            ActionResult<CityPopulationBootstrapSummaryDto> actionResult = await controller.InitializeCityPopulation(
                request: new InitializeCityPopulationRequest(
                    CityId: cityId,
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 6,
                        day: 1),
                    CreatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 6,
                        day: 1,
                        hour: 10,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    PeopleCount: 60,
                    RandomSeed: 77,
                    Environment: new CityPopulationEnvironmentDto(
                        ClimateZone: "Continental",
                        Hemisphere: "Northern",
                        UtcOffsetMinutes: 180),
                    Tuning: new CityPopulationBootstrapTuningDto(
                        HousingPressurePercent: 70,
                        EconomicStabilityPercent: 65,
                        SocialVolatilityPercent: 40,
                        FamilyFormationPercent: 55),
                    CityAnchors:
                    [
                        new CityAnchorSeedDto(
                            CityAnchorId: anchorId,
                            DistrictId: Guid.Parse("ecec2825-6608-433d-ae39-8e9968e56c58"),
                            AccessRoadNodeId: Guid.Parse("3665d130-99bb-4318-af22-fe475d9ae312"),
                            Name: "Hospital",
                            Type: "Healthcare",
                            Capacity: 80,
                            PositionX: 12.5m,
                            PositionY: 18.2m,
                            CreatedAtUtc: new DateTimeOffset(
                                year: 2048,
                                month: 5,
                                day: 31,
                                hour: 23,
                                minute: 0,
                                second: 0,
                                offset: TimeSpan.Zero))
                    ],
                    ResidentialBuildings:
                    [
                        new ResidentialBuildingSeedDto(
                            ResidentialBuildingId: buildingId,
                            DistrictId: Guid.Parse("ecec2825-6608-433d-ae39-8e9968e56c58"),
                            ResidentCapacity: 120)
                    ]),
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            CityPopulationBootstrapSummaryDto response = Assert.IsType<CityPopulationBootstrapSummaryDto>(ok.Value);
            Assert.Equal(
                expected: cityId,
                actual: response.CityId);
            Assert.Equal(
                expected: 60,
                actual: response.GeneratedPeopleCount);
        }

        [Fact]
        public async Task SyncEnvironmentAndEmploymentOperations_SendExpectedCommands()
        {
            var cityId = Guid.Parse("c4d117ca-b0e7-4142-a727-29e25708f905");
            var residentId = Guid.Parse("f7289757-6610-4af1-a7aa-768b89b94073");
            var workplaceId = Guid.Parse("6947d545-13a3-4b3a-ac34-a4208f9c3365");
            var sender = new FakeSender();
            sender.Handle<SyncCityEnvironmentCommand, SyncCityEnvironmentResult>(_
                => new SyncCityEnvironmentResult(SyncCityEnvironmentStatus.Applied));
            sender.Handle<HireCityResidentCommand, CityEmploymentOperationResultDto>(_
                => CreateEmploymentOperationResultDto());
            sender.Handle<FireCityResidentCommand, CityEmploymentOperationResultDto>(_
                => CreateEmploymentOperationResultDto("Fire"));
            sender.Handle<RetireCityResidentCommand, CityEmploymentOperationResultDto>(_
                => CreateEmploymentOperationResultDto("Retire"));
            var controller = new ClassicCityEmploymentController(sender);
            var stateController = new ClassicCityPopulationStateController(sender);

            IActionResult syncResult = await stateController.SyncCityEnvironment(
                cityId: cityId,
                request: new SyncCityEnvironmentRequest(
                    ClimateZone: "Steppe",
                    Hemisphere: "Northern",
                    UtcOffsetMinutes: 120),
                cancellationToken: CancellationToken.None);
            await controller.HireResident(
                cityId: cityId,
                request: new CityEmploymentOperationRequest(
                    ResidentId: residentId,
                    JobTitle: "Operator",
                    WorkplaceId: workplaceId,
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 6,
                        day: 1)),
                cancellationToken: CancellationToken.None);
            await controller.FireResident(
                cityId: cityId,
                request: new CityEmploymentOperationRequest(
                    ResidentId: residentId,
                    JobTitle: null,
                    WorkplaceId: null,
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 6,
                        day: 2)),
                cancellationToken: CancellationToken.None);
            await controller.RetireResident(
                cityId: cityId,
                request: new CityEmploymentOperationRequest(
                    ResidentId: residentId,
                    JobTitle: null,
                    WorkplaceId: null,
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 6,
                        day: 3)),
                cancellationToken: CancellationToken.None);

            Assert.IsType<NoContentResult>(syncResult);
            SyncCityEnvironmentCommand syncCommand = Assert.IsType<SyncCityEnvironmentCommand>(sender.Requests[0]);
            Assert.Equal(
                expected: "Steppe",
                actual: syncCommand.ClimateZone);
            Assert.Equal(
                expected: 120,
                actual: syncCommand.UtcOffsetMinutes);

            HireCityResidentCommand hireCommand = Assert.IsType<HireCityResidentCommand>(sender.Requests[1]);
            Assert.Equal(
                expected: "Operator",
                actual: hireCommand.JobTitle);
            Assert.Equal(
                expected: workplaceId,
                actual: hireCommand.WorkplaceId);

            FireCityResidentCommand fireCommand = Assert.IsType<FireCityResidentCommand>(sender.Requests[2]);
            Assert.Equal(
                expected: residentId,
                actual: fireCommand.ResidentId);

            RetireCityResidentCommand retireCommand = Assert.IsType<RetireCityResidentCommand>(sender.Requests[3]);
            Assert.Equal(
                expected: new DateOnly(
                    year: 2048,
                    month: 6,
                    day: 3),
                actual: retireCommand.CurrentDate);
        }

        [Fact]
        public async Task CivilRegistryOperations_SendExpectedCommands()
        {
            var cityId = Guid.Parse("96508e68-194a-4078-80d5-6b6fe2cf2ce0");
            var residentId = Guid.Parse("28f443e4-f6b0-4b7f-bbcb-10307ffcbd89");
            var secondResidentId = Guid.Parse("671814b3-a061-4ec5-a766-64726c1af1a0");
            var sender = new FakeSender();
            sender.Handle<RegisterCityMarriageCommand, CityCivilRegistryOperationResultDto>(_
                => CreateCivilRegistryOperationResultDto());
            sender.Handle<RegisterCityDivorceCommand, CityCivilRegistryOperationResultDto>(_
                => CreateCivilRegistryOperationResultDto("Divorce"));
            var controller = new ClassicCityCivilRegistryController(sender);
            await controller.RegisterMarriage(
                cityId: cityId,
                request: new CityCivilRegistryOperationRequest(
                    FirstResidentId: residentId,
                    SecondResidentId: secondResidentId,
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 6,
                        day: 4)),
                cancellationToken: CancellationToken.None);
            await controller.RegisterDivorce(
                cityId: cityId,
                request: new CityCivilRegistryOperationRequest(
                    FirstResidentId: residentId,
                    SecondResidentId: secondResidentId,
                    CurrentDate: new DateOnly(
                        year: 2048,
                        month: 6,
                        day: 5)),
                cancellationToken: CancellationToken.None);

            RegisterCityMarriageCommand marriageCommand =
                Assert.IsType<RegisterCityMarriageCommand>(sender.Requests[0]);
            Assert.Equal(
                expected: secondResidentId,
                actual: marriageCommand.SecondResidentId);

            RegisterCityDivorceCommand divorceCommand = Assert.IsType<RegisterCityDivorceCommand>(sender.Requests[1]);
            Assert.Equal(
                expected: new DateOnly(
                    year: 2048,
                    month: 6,
                    day: 5),
                actual: divorceCommand.CurrentDate);
        }
    }
}
