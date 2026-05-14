using Matrix.Population.Api.Controllers.Scenarios.ClassicCity;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.RegisterDivorce;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.RegisterMarriage;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.EnrollResident;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.GraduateResident;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.WithdrawResident;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.FireResident;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.HireResident;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.RetireResident;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityEnvironment;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.Population.Api.Tests.TestSupport.PopulationApiTestSupport;

namespace Matrix.Population.Api.Tests.Controllers.Scenarios.ClassicCity;

public sealed class PopulationMutationControllerTests
{
    [Fact]
    public async Task InitializeCityPopulation_MapsNestedRequest()
    {
        Guid cityId = Guid.Parse("d22b7a64-245d-4d96-8fb7-e4a0130686a4");
        Guid anchorId = Guid.Parse("4d679724-c724-4b9b-b402-cc7e303d42d8");
        Guid buildingId = Guid.Parse("a63f03d6-3320-4a95-8bb3-859f50f939aa");
        var sender = new FakeSender();
        sender.Handle<InitializeCityPopulationCommand, CityPopulationBootstrapSummaryDto>(command =>
        {
            Assert.Equal(cityId, command.CityId);
            Assert.Equal(new DateOnly(2048, 6, 1), command.CurrentDate);
            Assert.Equal(60, command.PeopleCount);
            Assert.Equal(77, command.RandomSeed);
            Assert.Equal("Continental", command.Environment.ClimateZone);
            Assert.Equal("Northern", command.Environment.Hemisphere);
            Assert.Equal(180, command.Environment.UtcOffsetMinutes);
            Assert.Equal(70, command.Tuning.HousingPressurePercent);
            Assert.Equal(65, command.Tuning.EconomicStabilityPercent);
            Assert.Equal(40, command.Tuning.SocialVolatilityPercent);
            Assert.Equal(55, command.Tuning.FamilyFormationPercent);

            CityAnchorSeedItem anchor = Assert.Single(command.CityAnchors);
            Assert.Equal(anchorId, anchor.CityAnchorId);
            Assert.Equal("Hospital", anchor.Name);

            ResidentialBuildingSeedItem building = Assert.Single(command.ResidentialBuildings);
            Assert.Equal(buildingId, building.ResidentialBuildingId);
            Assert.Equal(120, building.ResidentCapacity);

            return CreateBootstrapSummaryDto(cityId);
        });
        var controller = new PopulationController(sender);

        ActionResult<CityPopulationBootstrapSummaryDto> actionResult = await controller.InitializeCityPopulation(
            request: new InitializeCityPopulationRequest(
                CityId: cityId,
                CurrentDate: new DateOnly(2048, 6, 1),
                CreatedAtUtc: new DateTimeOffset(2048, 6, 1, 10, 0, 0, TimeSpan.Zero),
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
                        CreatedAtUtc: new DateTimeOffset(2048, 5, 31, 23, 0, 0, TimeSpan.Zero))
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
        Assert.Equal(cityId, response.CityId);
        Assert.Equal(60, response.GeneratedPeopleCount);
    }

    [Fact]
    public async Task SyncEnvironmentAndEmploymentOperations_SendExpectedCommands()
    {
        Guid cityId = Guid.Parse("c4d117ca-b0e7-4142-a727-29e25708f905");
        Guid residentId = Guid.Parse("f7289757-6610-4af1-a7aa-768b89b94073");
        Guid workplaceId = Guid.Parse("6947d545-13a3-4b3a-ac34-a4208f9c3365");
        var sender = new FakeSender();
        sender.Handle<SyncCityEnvironmentCommand, SyncCityEnvironmentResult>(_ => new SyncCityEnvironmentResult(SyncCityEnvironmentStatus.Applied));
        sender.Handle<HireCityResidentCommand, CityEmploymentOperationResultDto>(_ => CreateEmploymentOperationResultDto("Hire"));
        sender.Handle<FireCityResidentCommand, CityEmploymentOperationResultDto>(_ => CreateEmploymentOperationResultDto("Fire"));
        sender.Handle<RetireCityResidentCommand, CityEmploymentOperationResultDto>(_ => CreateEmploymentOperationResultDto("Retire"));
        var controller = new PopulationController(sender);

        IActionResult syncResult = await controller.SyncCityEnvironment(
            cityId: cityId,
            request: new SyncCityEnvironmentRequest(
                ClimateZone: "Steppe",
                Hemisphere: "Northern",
                UtcOffsetMinutes: 120),
            cancellationToken: CancellationToken.None);
        await controller.HireResident(
            cityId,
            new CityEmploymentOperationRequest(
                ResidentId: residentId,
                JobTitle: "Operator",
                WorkplaceId: workplaceId,
                CurrentDate: new DateOnly(2048, 6, 1)),
            CancellationToken.None);
        await controller.FireResident(
            cityId,
            new CityEmploymentOperationRequest(
                ResidentId: residentId,
                JobTitle: null,
                WorkplaceId: null,
                CurrentDate: new DateOnly(2048, 6, 2)),
            CancellationToken.None);
        await controller.RetireResident(
            cityId,
            new CityEmploymentOperationRequest(
                ResidentId: residentId,
                JobTitle: null,
                WorkplaceId: null,
                CurrentDate: new DateOnly(2048, 6, 3)),
            CancellationToken.None);

        Assert.IsType<NoContentResult>(syncResult);
        SyncCityEnvironmentCommand syncCommand = Assert.IsType<SyncCityEnvironmentCommand>(sender.Requests[0]);
        Assert.Equal("Steppe", syncCommand.ClimateZone);
        Assert.Equal(120, syncCommand.UtcOffsetMinutes);

        HireCityResidentCommand hireCommand = Assert.IsType<HireCityResidentCommand>(sender.Requests[1]);
        Assert.Equal("Operator", hireCommand.JobTitle);
        Assert.Equal(workplaceId, hireCommand.WorkplaceId);

        FireCityResidentCommand fireCommand = Assert.IsType<FireCityResidentCommand>(sender.Requests[2]);
        Assert.Equal(residentId, fireCommand.ResidentId);

        RetireCityResidentCommand retireCommand = Assert.IsType<RetireCityResidentCommand>(sender.Requests[3]);
        Assert.Equal(new DateOnly(2048, 6, 3), retireCommand.CurrentDate);
    }

    [Fact]
    public async Task EducationAndCivilRegistryOperations_SendExpectedCommands()
    {
        Guid cityId = Guid.Parse("96508e68-194a-4078-80d5-6b6fe2cf2ce0");
        Guid residentId = Guid.Parse("28f443e4-f6b0-4b7f-bbcb-10307ffcbd89");
        Guid institutionId = Guid.Parse("bdf4de49-1c59-46ec-b54f-8d4086eaee90");
        Guid secondResidentId = Guid.Parse("671814b3-a061-4ec5-a766-64726c1af1a0");
        var sender = new FakeSender();
        sender.Handle<EnrollCityResidentCommand, CityEducationOperationResultDto>(_ => CreateEducationOperationResultDto("Enroll"));
        sender.Handle<GraduateCityResidentCommand, CityEducationOperationResultDto>(_ => CreateEducationOperationResultDto("Graduate"));
        sender.Handle<WithdrawCityResidentFromStudyCommand, CityEducationOperationResultDto>(_ => CreateEducationOperationResultDto("Withdraw"));
        sender.Handle<RegisterCityMarriageCommand, CityCivilRegistryOperationResultDto>(_ => CreateCivilRegistryOperationResultDto("Marriage"));
        sender.Handle<RegisterCityDivorceCommand, CityCivilRegistryOperationResultDto>(_ => CreateCivilRegistryOperationResultDto("Divorce"));
        var controller = new PopulationController(sender);

        await controller.EnrollResident(
            cityId,
            new CityEducationOperationRequest(
                ResidentId: residentId,
                TargetEducationLevel: null,
                InstitutionId: institutionId,
                CurrentDate: new DateOnly(2048, 6, 1)),
            CancellationToken.None);
        await controller.GraduateResident(
            cityId,
            new CityEducationOperationRequest(
                ResidentId: residentId,
                TargetEducationLevel: "Higher",
                InstitutionId: institutionId,
                CurrentDate: new DateOnly(2048, 6, 2)),
            CancellationToken.None);
        await controller.WithdrawResident(
            cityId,
            new CityEducationOperationRequest(
                ResidentId: residentId,
                TargetEducationLevel: null,
                InstitutionId: null,
                CurrentDate: new DateOnly(2048, 6, 3)),
            CancellationToken.None);
        await controller.RegisterMarriage(
            cityId,
            new CityCivilRegistryOperationRequest(
                FirstResidentId: residentId,
                SecondResidentId: secondResidentId,
                CurrentDate: new DateOnly(2048, 6, 4)),
            CancellationToken.None);
        await controller.RegisterDivorce(
            cityId,
            new CityCivilRegistryOperationRequest(
                FirstResidentId: residentId,
                SecondResidentId: secondResidentId,
                CurrentDate: new DateOnly(2048, 6, 5)),
            CancellationToken.None);

        EnrollCityResidentCommand enrollCommand = Assert.IsType<EnrollCityResidentCommand>(sender.Requests[0]);
        Assert.Equal(institutionId, enrollCommand.InstitutionId);

        GraduateCityResidentCommand graduateCommand = Assert.IsType<GraduateCityResidentCommand>(sender.Requests[1]);
        Assert.Equal("Higher", graduateCommand.TargetEducationLevel);

        WithdrawCityResidentFromStudyCommand withdrawCommand = Assert.IsType<WithdrawCityResidentFromStudyCommand>(sender.Requests[2]);
        Assert.Equal(new DateOnly(2048, 6, 3), withdrawCommand.CurrentDate);

        RegisterCityMarriageCommand marriageCommand = Assert.IsType<RegisterCityMarriageCommand>(sender.Requests[3]);
        Assert.Equal(secondResidentId, marriageCommand.SecondResidentId);

        RegisterCityDivorceCommand divorceCommand = Assert.IsType<RegisterCityDivorceCommand>(sender.Requests[4]);
        Assert.Equal(new DateOnly(2048, 6, 5), divorceCommand.CurrentDate);
    }
}
