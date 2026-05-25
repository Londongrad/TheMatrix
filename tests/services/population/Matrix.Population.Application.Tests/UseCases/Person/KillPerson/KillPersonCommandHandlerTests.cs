using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Population.Application.UseCases.Person.KillPerson;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.UseCases.Person.KillPerson
{
    public sealed class KillPersonCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenPersonDoesNotExist_ThrowsNotFound()
        {
            KillPersonCommandHandler handler = CreateHandler(personReadRepository: new FakePersonReadRepository());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new KillPersonCommand(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Population.Person.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
        }

        [Fact]
        public async Task Handle_WhenPersonHasNoCity_KillsPersonAndSkipsClassicCitySideEffects()
        {
            Domain.Entities.Person person = CreatePerson(
                personId: Guid.Parse("11111111-aaaa-bbbb-cccc-111111111111"),
                birthDate: new DateOnly(
                    year: 2030,
                    month: 5,
                    day: 3));
            var personReadRepository = new FakePersonReadRepository
            {
                PersonById = person
            };
            personReadRepository.PersonsById[person.Id] = person;
            var personWriteRepository = new FakePersonWriteRepository();
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var activityJournalService = new FakeCityPopulationActivityJournalService();
            var unitOfWork = new FakeUnitOfWork();
            KillPersonCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                personWriteRepository: personWriteRepository,
                summaryProjectionService: summaryProjectionService,
                activityJournalService: activityJournalService,
                unitOfWork: unitOfWork);

            PersonDto result = await handler.Handle(
                request: new KillPersonCommand(person.Id.Value),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: LifeStatus.Deceased,
                actual: person.LifeStatus);
            Assert.Equal(
                expected: "Deceased",
                actual: result.LifeStatus);
            Assert.Single(personWriteRepository.UpdatedPersons);
            Assert.Same(
                expected: person,
                actual: personWriteRepository.UpdatedPersons[0]);
            Assert.Empty(summaryProjectionService.RebuildCalls);
            Assert.Empty(activityJournalService.Entries);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenMarriedPersonHasSpouseInSameCity_MarksSpouseWidowedAndRecordsActivities()
        {
            var personId = PersonId.From(Guid.Parse("22222222-aaaa-bbbb-cccc-222222222222"));
            var spouseId = PersonId.From(Guid.Parse("33333333-aaaa-bbbb-cccc-333333333333"));
            var householdId = Guid.Parse("44444444-aaaa-bbbb-cccc-444444444444");
            Domain.Entities.Person person = CreatePerson(
                personId: personId.Value,
                householdId: householdId,
                maritalStatus: MaritalStatus.Married,
                spouseId: spouseId,
                birthDate: new DateOnly(
                    year: 2029,
                    month: 5,
                    day: 3));
            Domain.Entities.Person spouse = CreatePerson(
                personId: spouseId.Value,
                householdId: householdId,
                firstName: "Trinity",
                sex: Sex.Female,
                maritalStatus: MaritalStatus.Married,
                spouseId: personId,
                birthDate: new DateOnly(
                    year: 2030,
                    month: 5,
                    day: 3));
            var cityId = CityId.From(Guid.Parse("55555555-aaaa-bbbb-cccc-555555555555"));
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
                    lastProcessedDate: new DateOnly(
                        year: 2048,
                        month: 4,
                        day: 28),
                    updatedAtUtc: UtcNow)
            };
            var personWriteRepository = new FakePersonWriteRepository();
            var activityJournalService = new FakeCityPopulationActivityJournalService();
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            KillPersonCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                cityPopulationPersonReadRepository: cityPersonReadRepository,
                progressionStateRepository: progressionStateRepository,
                activityJournalService: activityJournalService,
                summaryProjectionService: summaryProjectionService,
                personWriteRepository: personWriteRepository);

            PersonDto result = await handler.Handle(
                request: new KillPersonCommand(person.Id.Value),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "Deceased",
                actual: result.LifeStatus);
            Assert.Equal(
                expected: MaritalStatus.Widowed,
                actual: spouse.MaritalStatus);
            Assert.Equal(
                expected: 2,
                actual: personWriteRepository.UpdatedPersons.Count);
            Assert.Contains(
                expected: spouse,
                collection: personWriteRepository.UpdatedPersons);
            Assert.Contains(
                expected: person,
                collection: personWriteRepository.UpdatedPersons);
            Assert.Single(summaryProjectionService.RebuildCalls);
            Assert.Equal(
                expected: (cityId, new DateOnly(
                               year: 2048,
                               month: 4,
                               day: 28)),
                actual: summaryProjectionService.RebuildCalls[0]);
            Assert.Equal(
                expected: 2,
                actual: activityJournalService.Entries.Count);
            Assert.Contains(
                collection: activityJournalService.Entries,
                filter: x => x.EventType == CityPopulationActivityEventType.ResidentBecameWidowed);
            Assert.Contains(
                collection: activityJournalService.Entries,
                filter: x => x.EventType == CityPopulationActivityEventType.ResidentDied);
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
                personReadRepository: personReadRepository ?? new FakePersonReadRepository(),
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository ??
                                                    new FakeCityPopulationPersonReadRepository(),
                cityPopulationProgressionStateRepository: progressionStateRepository ??
                                                          new FakeCityPopulationProgressionStateRepository(),
                cityPopulationActivityJournalService: activityJournalService ??
                                                      new FakeCityPopulationActivityJournalService(),
                cityPopulationSummaryProjectionService: summaryProjectionService ??
                                                        new FakeCityPopulationSummaryProjectionService(),
                marriageDomainService: new MarriageDomainService(),
                personWriteRepository: personWriteRepository ?? new FakePersonWriteRepository(),
                timeProvider: new FakeTimeProvider(UtcNow),
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
        }
    }
}
