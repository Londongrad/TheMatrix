using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Services;
using Matrix.Population.Application.UseCases.Person.ResurrectPerson;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.UseCases.Person.ResurrectPerson
{
    public sealed class ResurrectPersonCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenPersonDoesNotExist_ThrowsNotFound()
        {
            ResurrectPersonCommandHandler handler = CreateHandler(personReadRepository: new FakePersonReadRepository());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new ResurrectPersonCommand(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Population.Person.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
        }

        [Fact]
        public async Task Handle_WhenPersonHasNoCity_ResurrectsAndSkipsClassicCitySideEffects()
        {
            Domain.Entities.Person person = CreatePerson(
                lifeStatus: LifeStatus.Deceased,
                health: 80,
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
            ResurrectPersonCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                personWriteRepository: personWriteRepository,
                summaryProjectionService: summaryProjectionService,
                activityJournalService: activityJournalService,
                unitOfWork: unitOfWork);

            PersonDto result = await handler.Handle(
                request: new ResurrectPersonCommand(person.Id.Value),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: LifeStatus.Alive,
                actual: person.LifeStatus);
            Assert.Equal(
                expected: "Alive",
                actual: result.LifeStatus);
            Assert.Single(personWriteRepository.UpdatedPersons);
            Assert.Empty(summaryProjectionService.RebuildCalls);
            Assert.Empty(activityJournalService.Entries);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenPersonBelongsToCity_UsesProgressionDateAndRecordsActivity()
        {
            Domain.Entities.Person person = CreatePerson(
                personId: Guid.Parse("11111111-aaaa-bbbb-cccc-111111111111"),
                lifeStatus: LifeStatus.Deceased,
                health: 80,
                birthDate: new DateOnly(
                    year: 2030,
                    month: 5,
                    day: 3));
            var cityId = CityId.From(Guid.Parse("22222222-aaaa-bbbb-cccc-222222222222"));
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
                    lastProcessedDate: new DateOnly(
                        year: 2048,
                        month: 4,
                        day: 30),
                    updatedAtUtc: UtcNow)
            };
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var activityJournalService = new FakeCityPopulationActivityJournalService();
            var factsWriter = new FakePopulationResidentFactsOutboxWriter();
            var medicalStateWriter = new FakePopulationResidentMedicalStateOutboxWriter();
            ResurrectPersonCommandHandler handler = CreateHandler(
                personReadRepository: personReadRepository,
                cityPopulationPersonReadRepository: cityPopulationPersonReadRepository,
                progressionStateRepository: progressionStateRepository,
                summaryProjectionService: summaryProjectionService,
                activityJournalService: activityJournalService,
                residentFactsOutboxWriter: factsWriter,
                residentMedicalStateOutboxWriter: medicalStateWriter);

            PersonDto result = await handler.Handle(
                request: new ResurrectPersonCommand(person.Id.Value),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "Alive",
                actual: result.LifeStatus);
            Assert.Single(summaryProjectionService.RebuildCalls);
            Assert.Equal(
                expected: (cityId, new DateOnly(
                               year: 2048,
                               month: 4,
                               day: 30)),
                actual: summaryProjectionService.RebuildCalls[0]);
            CityPopulationActivityWriteModel activity = Assert.Single(activityJournalService.Entries);
            Assert.Equal(
                expected: CityPopulationActivityEventType.ResidentResurrected,
                actual: activity.EventType);
            Assert.Equal(
                expected: new DateOnly(
                    year: 2048,
                    month: 4,
                    day: 30),
                actual: activity.CurrentDate);
            Assert.Equal(9, Assert.Single(factsWriter.Batches).SourceRevision);
            Assert.True(Assert.Single(factsWriter.Batches[0].Residents).IsAlive);
            Assert.Equal(100, Assert.Single(medicalStateWriter.Batches[0].Residents).HealthScore);
            Assert.Equal(person.LifecycleRevision,
                Assert.Single(medicalStateWriter.Batches[0].Residents).LifecycleRevision);
        }

        private static ResurrectPersonCommandHandler CreateHandler(
            FakePersonReadRepository? personReadRepository = null,
            FakeCityPopulationPersonReadRepository? cityPopulationPersonReadRepository = null,
            FakeCityPopulationProgressionStateRepository? progressionStateRepository = null,
            FakeCityPopulationActivityJournalService? activityJournalService = null,
            FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
            FakePersonWriteRepository? personWriteRepository = null,
            FakeUnitOfWork? unitOfWork = null,
            FakePopulationResidentFactsOutboxWriter? residentFactsOutboxWriter = null,
            FakePopulationResidentMedicalStateOutboxWriter? residentMedicalStateOutboxWriter = null)
        {
            FakePersonReadRepository resolvedPersonReadRepository =
                personReadRepository ?? new FakePersonReadRepository();
            FakePersonWriteRepository resolvedPersonWriteRepository =
                personWriteRepository ?? new FakePersonWriteRepository();

            return new ResurrectPersonCommandHandler(
                personReadRepository: resolvedPersonReadRepository,
                personWriteRepository: resolvedPersonWriteRepository,
                lifecycleExtensions:
                [
                    new ClassicCityPersonLifecycleExtension(
                        personReadRepository: resolvedPersonReadRepository,
                        cityPopulationPersonReadRepository: cityPopulationPersonReadRepository ??
                                                            new FakeCityPopulationPersonReadRepository(),
                        cityPopulationProgressionStateRepository: progressionStateRepository ??
                                                                  new FakeCityPopulationProgressionStateRepository(),
                        cityPopulationActivityJournalService: activityJournalService ??
                                                              new FakeCityPopulationActivityJournalService(),
                        cityPopulationSummaryProjectionService: summaryProjectionService ??
                                                                new FakeCityPopulationSummaryProjectionService(),
                        marriageDomainService: new MarriageDomainService(),
                        personWriteRepository: resolvedPersonWriteRepository,
                        residentFactsOutboxWriter: residentFactsOutboxWriter ??
                                                   new FakePopulationResidentFactsOutboxWriter(),
                        residentMedicalStateOutboxWriter: residentMedicalStateOutboxWriter ??
                                                          new FakePopulationResidentMedicalStateOutboxWriter())
                ],
                timeProvider: new FakeTimeProvider(UtcNow),
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
        }
    }
}
