using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Population.Application.UseCases.Person.KillPerson;
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

namespace Matrix.Population.Application.Tests.UseCases.Person.KillPerson;

public sealed class KillPersonCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenPersonDoesNotExist_ThrowsNotFound()
    {
        var handler = CreateHandler(personReadRepository: new FakePersonReadRepository());

        MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new KillPersonCommand(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            CancellationToken.None));

        Assert.Equal("Population.Person.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenPersonHasNoCity_KillsPersonAndSkipsClassicCitySideEffects()
    {
        Matrix.Population.Domain.Entities.Person person = CreatePerson(
            personId: Guid.Parse("11111111-aaaa-bbbb-cccc-111111111111"),
            birthDate: new DateOnly(2030, 5, 3));
        var personReadRepository = new FakePersonReadRepository
        {
            PersonById = person
        };
        personReadRepository.PersonsById[person.Id] = person;
        var personWriteRepository = new FakePersonWriteRepository();
        var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
        var activityJournalService = new FakeCityPopulationActivityJournalService();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            personReadRepository: personReadRepository,
            personWriteRepository: personWriteRepository,
            summaryProjectionService: summaryProjectionService,
            activityJournalService: activityJournalService,
            unitOfWork: unitOfWork);

        var result = await handler.Handle(new KillPersonCommand(person.Id.Value), CancellationToken.None);

        Assert.Equal(LifeStatus.Deceased, person.LifeStatus);
        Assert.Equal("Deceased", result.LifeStatus);
        Assert.Single(personWriteRepository.UpdatedPersons);
        Assert.Same(person, personWriteRepository.UpdatedPersons[0]);
        Assert.Empty(summaryProjectionService.RebuildCalls);
        Assert.Empty(activityJournalService.Entries);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenMarriedPersonHasSpouseInSameCity_MarksSpouseWidowedAndRecordsActivities()
    {
        PersonId personId = PersonId.From(Guid.Parse("22222222-aaaa-bbbb-cccc-222222222222"));
        PersonId spouseId = PersonId.From(Guid.Parse("33333333-aaaa-bbbb-cccc-333333333333"));
        Guid householdId = Guid.Parse("44444444-aaaa-bbbb-cccc-444444444444");
        Matrix.Population.Domain.Entities.Person person = CreatePerson(
            personId: personId.Value,
            householdId: householdId,
            maritalStatus: MaritalStatus.Married,
            spouseId: spouseId,
            birthDate: new DateOnly(2029, 5, 3));
        Matrix.Population.Domain.Entities.Person spouse = CreatePerson(
            personId: spouseId.Value,
            householdId: householdId,
            firstName: "Trinity",
            sex: Sex.Female,
            maritalStatus: MaritalStatus.Married,
            spouseId: personId,
            birthDate: new DateOnly(2030, 5, 3));
        CityId cityId = CityId.From(Guid.Parse("55555555-aaaa-bbbb-cccc-555555555555"));
        var personReadRepository = new FakePersonReadRepository
        {
            PersonById = person
        };
        personReadRepository.PersonsById[person.Id] = person;
        personReadRepository.PersonsById[spouse.Id] = spouse;
        var cityPersonReadRepository = new FakeCityPopulationPersonReadRepository
        {
            CityIdByPersonId = cityId
        };
        var progressionStateRepository = new FakeCityPopulationProgressionStateRepository
        {
            State = CityPopulationProgressionState.Create(
                cityId: cityId,
                lastProcessedTickId: 12,
                lastProcessedDate: new DateOnly(2048, 4, 28),
                updatedAtUtc: UtcNow)
        };
        var personWriteRepository = new FakePersonWriteRepository();
        var activityJournalService = new FakeCityPopulationActivityJournalService();
        var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
        var handler = CreateHandler(
            personReadRepository: personReadRepository,
            cityPopulationPersonReadRepository: cityPersonReadRepository,
            progressionStateRepository: progressionStateRepository,
            activityJournalService: activityJournalService,
            summaryProjectionService: summaryProjectionService,
            personWriteRepository: personWriteRepository);

        var result = await handler.Handle(new KillPersonCommand(person.Id.Value), CancellationToken.None);

        Assert.Equal("Deceased", result.LifeStatus);
        Assert.Equal(MaritalStatus.Widowed, spouse.MaritalStatus);
        Assert.Equal(2, personWriteRepository.UpdatedPersons.Count);
        Assert.Contains(spouse, personWriteRepository.UpdatedPersons);
        Assert.Contains(person, personWriteRepository.UpdatedPersons);
        Assert.Single(summaryProjectionService.RebuildCalls);
        Assert.Equal((cityId, new DateOnly(2048, 4, 28)), summaryProjectionService.RebuildCalls[0]);
        Assert.Equal(2, activityJournalService.Entries.Count);
        Assert.Contains(activityJournalService.Entries, x => x.EventType == CityPopulationActivityEventType.ResidentBecameWidowed);
        Assert.Contains(activityJournalService.Entries, x => x.EventType == CityPopulationActivityEventType.ResidentDied);
    }

    private static KillPersonCommandHandler CreateHandler(
        FakePersonReadRepository? personReadRepository = null,
        FakeCityPopulationPersonReadRepository? cityPopulationPersonReadRepository = null,
        FakeCityPopulationProgressionStateRepository? progressionStateRepository = null,
        FakeCityPopulationActivityJournalService? activityJournalService = null,
        FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
        FakePersonWriteRepository? personWriteRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new KillPersonCommandHandler(
            personReadRepository ?? new FakePersonReadRepository(),
            cityPopulationPersonReadRepository ?? new FakeCityPopulationPersonReadRepository(),
            progressionStateRepository ?? new FakeCityPopulationProgressionStateRepository(),
            activityJournalService ?? new FakeCityPopulationActivityJournalService(),
            summaryProjectionService ?? new FakeCityPopulationSummaryProjectionService(),
            new MarriageDomainService(),
            personWriteRepository ?? new FakePersonWriteRepository(),
            new FakeTimeProvider(UtcNow),
            unitOfWork ?? new FakeUnitOfWork());
    }
}
