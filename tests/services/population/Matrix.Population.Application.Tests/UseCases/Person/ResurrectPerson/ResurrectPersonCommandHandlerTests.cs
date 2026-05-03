using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Population.Application.UseCases.Person.ResurrectPerson;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.UseCases.Person.ResurrectPerson;

public sealed class ResurrectPersonCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenPersonDoesNotExist_ThrowsNotFound()
    {
        var handler = CreateHandler(personReadRepository: new FakePersonReadRepository());

        MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new ResurrectPersonCommand(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            CancellationToken.None));

        Assert.Equal("Population.Person.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenPersonHasNoCity_ResurrectsAndSkipsClassicCitySideEffects()
    {
        Matrix.Population.Domain.Entities.Person person = CreatePerson(
            lifeStatus: LifeStatus.Deceased,
            health: 80,
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

        var result = await handler.Handle(new ResurrectPersonCommand(person.Id.Value), CancellationToken.None);

        Assert.Equal(LifeStatus.Alive, person.LifeStatus);
        Assert.Equal("Alive", result.LifeStatus);
        Assert.Single(personWriteRepository.UpdatedPersons);
        Assert.Empty(summaryProjectionService.RebuildCalls);
        Assert.Empty(activityJournalService.Entries);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenPersonBelongsToCity_UsesProgressionDateAndRecordsActivity()
    {
        Matrix.Population.Domain.Entities.Person person = CreatePerson(
            personId: Guid.Parse("11111111-aaaa-bbbb-cccc-111111111111"),
            lifeStatus: LifeStatus.Deceased,
            health: 80,
            birthDate: new DateOnly(2030, 5, 3));
        CityId cityId = CityId.From(Guid.Parse("22222222-aaaa-bbbb-cccc-222222222222"));
        var personReadRepository = new FakePersonReadRepository
        {
            PersonById = person
        };
        personReadRepository.PersonsById[person.Id] = person;
        var cityPopulationPersonReadRepository = new FakeCityPopulationPersonReadRepository
        {
            CityIdByPersonId = cityId
        };
        var progressionStateRepository = new FakeCityPopulationProgressionStateRepository
        {
            State = CityPopulationProgressionState.Create(
                cityId: cityId,
                lastProcessedTickId: 9,
                lastProcessedDate: new DateOnly(2048, 4, 30),
                updatedAtUtc: UtcNow)
        };
        var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
        var activityJournalService = new FakeCityPopulationActivityJournalService();
        var handler = CreateHandler(
            personReadRepository: personReadRepository,
            cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
            progressionStateRepository: progressionStateRepository,
            summaryProjectionService: summaryProjectionService,
            activityJournalService: activityJournalService);

        var result = await handler.Handle(new ResurrectPersonCommand(person.Id.Value), CancellationToken.None);

        Assert.Equal("Alive", result.LifeStatus);
        Assert.Single(summaryProjectionService.RebuildCalls);
        Assert.Equal((cityId, new DateOnly(2048, 4, 30)), summaryProjectionService.RebuildCalls[0]);
        CityPopulationActivityWriteModel activity = Assert.Single(activityJournalService.Entries);
        Assert.Equal(CityPopulationActivityEventType.ResidentResurrected, activity.EventType);
        Assert.Equal(new DateOnly(2048, 4, 30), activity.CurrentDate);
    }

    private static ResurrectPersonCommandHandler CreateHandler(
        FakePersonReadRepository? personReadRepository = null,
        FakeCityPopulationPersonReadRepository? cityPopulationPersonReadRepository = null,
        FakeCityPopulationProgressionStateRepository? progressionStateRepository = null,
        FakeCityPopulationActivityJournalService? activityJournalService = null,
        FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
        FakePersonWriteRepository? personWriteRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new ResurrectPersonCommandHandler(
            personReadRepository ?? new FakePersonReadRepository(),
            cityPopulationPersonReadRepository ?? new FakeCityPopulationPersonReadRepository(),
            progressionStateRepository ?? new FakeCityPopulationProgressionStateRepository(),
            activityJournalService ?? new FakeCityPopulationActivityJournalService(),
            summaryProjectionService ?? new FakeCityPopulationSummaryProjectionService(),
            personWriteRepository ?? new FakePersonWriteRepository(),
            new FakeTimeProvider(UtcNow),
            unitOfWork ?? new FakeUnitOfWork());
    }
}
