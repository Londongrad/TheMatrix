using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.RegisterDivorce;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.CivilRegistry.RegisterDivorce;

public sealed class RegisterCityDivorceCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenResidentsShareHousehold_SeparatesSpouses()
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
        var marriageDomainService = new MarriageDomainService();
        marriageDomainService.RegisterMarriage(
            person: firstResident,
            spouse: secondResident,
            currentDate: new DateOnly(2048, 5, 3));
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

        Assert.Equal("DivorceRegistered", result.Action);
        Assert.Equal(MaritalStatus.Single, firstResident.MaritalStatus);
        Assert.Equal(MaritalStatus.Single, secondResident.MaritalStatus);
        Assert.Null(firstResident.SpouseId);
        Assert.Null(secondResident.SpouseId);
        Assert.NotEqual(sharedHouseholdId, secondResident.HouseholdId);
        Household updatedSharedHousehold = Assert.Single(householdWriteRepository.UpdatedHouseholds);
        Assert.Equal(sharedHouseholdId, updatedSharedHousehold.Id);
        Assert.Equal(1, updatedSharedHousehold.Size.Value);
        var addedHousehold = Assert.Single(householdWriteRepository.AddedHouseholds);
        Assert.Equal(secondResident.HouseholdId, addedHousehold.Household.Id);
        Assert.Equal(HousingStatus.Housed, addedHousehold.Placement.HousingStatus);
        Assert.Equal(DistrictId.From(Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb")), addedHousehold.Placement.DistrictId);
        Assert.Equal(2, personWriteRepository.UpdatedPersons.Count);
        Assert.Single(summaryProjectionService.RebuildCalls);
        Assert.Single(activityJournalService.Entries);
        Assert.Null(result.FirstResident.CurrentSpouse);
        Assert.Null(result.SecondResident.CurrentSpouse);
        Assert.Equal(secondResident.HouseholdId.Value, result.SecondResident.CurrentHousing.HouseholdId);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenResidentsAreNotCurrentSpouses_ThrowsBusinessRule()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        HouseholdId firstHouseholdId = HouseholdId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        HouseholdId secondHouseholdId = HouseholdId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        Person firstResident = CreatePerson(
            personId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            householdId: firstHouseholdId.Value,
            firstName: "Neo",
            sex: Sex.Male);
        Person secondResident = CreatePerson(
            personId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            householdId: secondHouseholdId.Value,
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

        MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(
            async () => await handler.Handle(
                CreateCommand(cityId, firstResident.Id.Value, secondResident.Id.Value),
                CancellationToken.None));

        Assert.Equal("Population.CivilRegistry.ResidentsAreNotCurrentSpouses", exception.Code);
        Assert.Empty(personWriteRepository.UpdatedPersons);
        Assert.Empty(householdWriteRepository.UpdatedHouseholds);
        Assert.Empty(householdWriteRepository.AddedHouseholds);
        Assert.Empty(activityJournalService.Entries);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    private static RegisterCityDivorceCommandHandler CreateHandler(
        FakePersonReadRepository? personReadRepository = null,
        FakeCityPopulationPersonReadRepository? cityPopulationPersonReadRepository = null,
        FakeCityPopulationActivityJournalService? activityJournalService = null,
        FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
        FakePersonWriteRepository? personWriteRepository = null,
        FakeHouseholdWriteRepository? householdWriteRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new RegisterCityDivorceCommandHandler(
            personReadRepository ?? new FakePersonReadRepository(),
            cityPopulationPersonReadRepository ?? new FakeCityPopulationPersonReadRepository(),
            activityJournalService ?? new FakeCityPopulationActivityJournalService(),
            summaryProjectionService ?? new FakeCityPopulationSummaryProjectionService(),
            personWriteRepository ?? new FakePersonWriteRepository(),
            householdWriteRepository ?? new FakeHouseholdWriteRepository(),
            new MarriageDomainService(),
            unitOfWork ?? new FakeUnitOfWork());
    }

    private static RegisterCityDivorceCommand CreateCommand(Guid cityId, Guid firstResidentId, Guid secondResidentId)
    {
        return new RegisterCityDivorceCommand(
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
