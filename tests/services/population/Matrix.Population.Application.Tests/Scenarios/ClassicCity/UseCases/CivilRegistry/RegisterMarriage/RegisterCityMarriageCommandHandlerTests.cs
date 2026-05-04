using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.RegisterMarriage;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.CivilRegistry.RegisterMarriage;

public sealed class RegisterCityMarriageCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenResidentsAreInDifferentHouseholds_MergesIntoHousedHousehold()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        HouseholdId firstHouseholdId = HouseholdId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        HouseholdId secondHouseholdId = HouseholdId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        Person firstResident = CreatePerson(
            personId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            householdId: firstHouseholdId.Value,
            firstName: "Neo",
            lastName: "Anderson",
            sex: Sex.Male);
        Person secondResident = CreatePerson(
            personId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            householdId: secondHouseholdId.Value,
            firstName: "Trinity",
            lastName: "Unknown",
            sex: Sex.Female);
        var personReadRepository = new FakePersonReadRepository();
        personReadRepository.PersonsById[firstResident.Id] = firstResident;
        personReadRepository.PersonsById[secondResident.Id] = secondResident;
        var cityPopulationPersonReadRepository = new FakeCityPopulationPersonReadRepository();
        cityPopulationPersonReadRepository.CityIdByPersonIds[firstResident.Id] = CityId.From(cityId);
        cityPopulationPersonReadRepository.CityIdByPersonIds[secondResident.Id] = CityId.From(cityId);
        var personWriteRepository = new FakePersonWriteRepository();
        var householdWriteRepository = new FakeHouseholdWriteRepository();
        householdWriteRepository.HouseholdsById[firstHouseholdId] = CreateHousehold(firstHouseholdId, 1);
        householdWriteRepository.HouseholdsById[secondHouseholdId] = CreateHousehold(secondHouseholdId, 1);
        householdWriteRepository.PlacementsByHouseholdId[firstHouseholdId] = ClassicCityHouseholdPlacement.CreateHoused(
            householdId: firstHouseholdId,
            cityId: CityId.From(cityId),
            districtId: DistrictId.From(Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb")),
            residentialBuildingId: ResidentialBuildingId.From(Guid.Parse("cccccccc-1111-2222-3333-dddddddddddd")));
        householdWriteRepository.PlacementsByHouseholdId[secondHouseholdId] = ClassicCityHouseholdPlacement.CreateHomeless(
            householdId: secondHouseholdId,
            cityId: CityId.From(cityId));
        householdWriteRepository.ResidentCountByHouseholdId[firstHouseholdId] = 1;
        householdWriteRepository.ResidentCountByHouseholdId[secondHouseholdId] = 1;
        var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
        var activityJournalService = new FakeCityPopulationActivityJournalService();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            personReadRepository: personReadRepository,
            cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
            activityJournalService: activityJournalService,
            summaryProjectionService: summaryProjectionService,
            personWriteRepository: personWriteRepository,
            householdWriteRepository: householdWriteRepository,
            unitOfWork: unitOfWork);

        var result = await handler.Handle(
            CreateCommand(cityId, firstResident.Id.Value, secondResident.Id.Value),
            CancellationToken.None);

        Assert.Equal("MarriageRegistered", result.Action);
        Assert.Equal(MaritalStatus.Married, firstResident.MaritalStatus);
        Assert.Equal(MaritalStatus.Married, secondResident.MaritalStatus);
        Assert.Equal(firstResident.Id, secondResident.SpouseId);
        Assert.Equal(secondResident.Id, firstResident.SpouseId);
        Assert.Equal(firstHouseholdId, secondResident.HouseholdId);
        Household updatedTargetHousehold = Assert.Single(householdWriteRepository.UpdatedHouseholds);
        Assert.Equal(firstHouseholdId, updatedTargetHousehold.Id);
        Assert.Equal(2, updatedTargetHousehold.Size.Value);
        Household deletedSourceHousehold = Assert.Single(householdWriteRepository.DeletedHouseholds);
        Assert.Equal(secondHouseholdId, deletedSourceHousehold.Id);
        Assert.Equal(2, personWriteRepository.UpdatedPersons.Count);
        Assert.Single(summaryProjectionService.RebuildCalls);
        Assert.Single(activityJournalService.Entries);
        Assert.Equal(firstResident.Id.Value, result.FirstResident.Id);
        Assert.NotNull(result.FirstResident.CurrentSpouse);
        Assert.Equal(secondResident.Id.Value, result.FirstResident.CurrentSpouse!.Id);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenResidentsAlreadyShareHousehold_DoesNotMutateHouseholds()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        HouseholdId sharedHouseholdId = HouseholdId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        Person firstResident = CreatePerson(
            personId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            householdId: sharedHouseholdId.Value,
            firstName: "Neo",
            sex: Sex.Male);
        Person secondResident = CreatePerson(
            personId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            householdId: sharedHouseholdId.Value,
            firstName: "Trinity",
            sex: Sex.Female);
        var personReadRepository = new FakePersonReadRepository();
        personReadRepository.PersonsById[firstResident.Id] = firstResident;
        personReadRepository.PersonsById[secondResident.Id] = secondResident;
        var cityPopulationPersonReadRepository = new FakeCityPopulationPersonReadRepository();
        cityPopulationPersonReadRepository.CityIdByPersonIds[firstResident.Id] = CityId.From(cityId);
        cityPopulationPersonReadRepository.CityIdByPersonIds[secondResident.Id] = CityId.From(cityId);
        var personWriteRepository = new FakePersonWriteRepository();
        var householdWriteRepository = new FakeHouseholdWriteRepository();
        householdWriteRepository.HouseholdsById[sharedHouseholdId] = CreateHousehold(sharedHouseholdId, 2);
        householdWriteRepository.PlacementsByHouseholdId[sharedHouseholdId] = ClassicCityHouseholdPlacement.CreateHoused(
            householdId: sharedHouseholdId,
            cityId: CityId.From(cityId),
            districtId: DistrictId.From(Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb")),
            residentialBuildingId: ResidentialBuildingId.From(Guid.Parse("cccccccc-1111-2222-3333-dddddddddddd")));
        householdWriteRepository.ResidentCountByHouseholdId[sharedHouseholdId] = 2;
        var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
        var activityJournalService = new FakeCityPopulationActivityJournalService();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            personReadRepository: personReadRepository,
            cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
            activityJournalService: activityJournalService,
            summaryProjectionService: summaryProjectionService,
            personWriteRepository: personWriteRepository,
            householdWriteRepository: householdWriteRepository,
            unitOfWork: unitOfWork);

        var result = await handler.Handle(
            CreateCommand(cityId, firstResident.Id.Value, secondResident.Id.Value),
            CancellationToken.None);

        Assert.Equal("MarriageRegistered", result.Action);
        Assert.Empty(householdWriteRepository.UpdatedHouseholds);
        Assert.Empty(householdWriteRepository.DeletedHouseholds);
        Assert.Empty(householdWriteRepository.AddedHouseholds);
        Assert.Equal(sharedHouseholdId, firstResident.HouseholdId);
        Assert.Equal(sharedHouseholdId, secondResident.HouseholdId);
        Assert.Equal(2, personWriteRepository.UpdatedPersons.Count);
        Assert.Single(summaryProjectionService.RebuildCalls);
        Assert.Single(activityJournalService.Entries);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    private static RegisterCityMarriageCommandHandler CreateHandler(
        FakePersonReadRepository? personReadRepository = null,
        FakeCityPopulationPersonReadRepository? cityPopulationPersonReadRepository = null,
        FakeCityPopulationActivityJournalService? activityJournalService = null,
        FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
        FakePersonWriteRepository? personWriteRepository = null,
        FakeHouseholdWriteRepository? householdWriteRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new RegisterCityMarriageCommandHandler(
            personReadRepository ?? new FakePersonReadRepository(),
            cityPopulationPersonReadRepository ?? new FakeCityPopulationPersonReadRepository(),
            activityJournalService ?? new FakeCityPopulationActivityJournalService(),
            summaryProjectionService ?? new FakeCityPopulationSummaryProjectionService(),
            personWriteRepository ?? new FakePersonWriteRepository(),
            householdWriteRepository ?? new FakeHouseholdWriteRepository(),
            new MarriageDomainService(),
            unitOfWork ?? new FakeUnitOfWork());
    }

    private static RegisterCityMarriageCommand CreateCommand(Guid cityId, Guid firstResidentId, Guid secondResidentId)
    {
        return new RegisterCityMarriageCommand(
            CityId: cityId,
            FirstResidentId: firstResidentId,
            SecondResidentId: secondResidentId,
            CurrentDate: new DateOnly(2048, 5, 4));
    }

    private static Household CreateHousehold(HouseholdId householdId, int size)
    {
        return Household.Create(
            id: householdId,
            size: HouseholdSize.From(size),
            createdAtUtc: UtcNow);
    }
}
