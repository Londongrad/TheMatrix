using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class AdvanceCityPopulationCommandHandlerGuardTests
    {
        [Fact]
        public async Task Handle_WhenTickIsAlreadyProcessed_ReturnsDuplicate()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var progressionStateRepository = new FakeCityPopulationProgressionStateRepository
            {
                State = CityPopulationProgressionState.Create(
                    cityId: CityId.From(cityId),
                    lastProcessedTickId: 10,
                    lastProcessedDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 5),
                    updatedAtUtc: UtcNow)
            };
            var unitOfWork = new FakeUnitOfWork();
            AdvanceCityPopulationCommandHandler handler = CreateHandler(
                progressionStateRepository: progressionStateRepository,
                unitOfWork: unitOfWork);

            AdvanceCityPopulationResult result = await handler.Handle(
                request: CreateCommand(
                    cityId: cityId,
                    tickId: 10),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: AdvanceCityPopulationStatus.Duplicate,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: result.AffectedPeopleCount);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: progressionStateRepository.RequestedCityId);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.ExecuteTransactionCalls);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenDateMovesBackwards_ReturnsOutOfOrder()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var progressionStateRepository = new FakeCityPopulationProgressionStateRepository
            {
                State = CityPopulationProgressionState.Create(
                    cityId: CityId.From(cityId),
                    lastProcessedTickId: 8,
                    lastProcessedDate: new DateOnly(
                        year: 2048,
                        month: 5,
                        day: 6),
                    updatedAtUtc: UtcNow)
            };
            var unitOfWork = new FakeUnitOfWork();
            AdvanceCityPopulationCommandHandler handler = CreateHandler(
                progressionStateRepository: progressionStateRepository,
                unitOfWork: unitOfWork);

            AdvanceCityPopulationResult result = await handler.Handle(
                request: new AdvanceCityPopulationCommand(
                    CityId: cityId,
                    FromSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 5,
                        hour: 9,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    ToSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 5,
                        hour: 10,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero),
                    TickId: 9),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: AdvanceCityPopulationStatus.OutOfOrder,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: result.AffectedPeopleCount);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.ExecuteTransactionCalls);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenCityIsDeleted_ReturnsCityDeleted()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var deletionStateRepository = new FakeCityPopulationDeletionStateRepository
            {
                State = CityPopulationDeletionState.Create(
                    cityId: CityId.From(cityId),
                    deletedAtUtc: UtcNow,
                    updatedAtUtc: UtcNow)
            };
            var unitOfWork = new FakeUnitOfWork();
            AdvanceCityPopulationCommandHandler handler = CreateHandler(
                deletionStateRepository: deletionStateRepository,
                unitOfWork: unitOfWork);

            AdvanceCityPopulationResult result = await handler.Handle(
                request: CreateCommand(
                    cityId: cityId,
                    tickId: 11),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: AdvanceCityPopulationStatus.CityDeleted,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: result.AffectedPeopleCount);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: deletionStateRepository.RequestedCityId);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.ExecuteTransactionCalls);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenCityIsArchived_ReturnsCityArchived()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var archiveStateRepository = new FakeCityPopulationArchiveStateRepository
            {
                State = CityPopulationArchiveState.Create(
                    cityId: CityId.From(cityId),
                    archivedAtUtc: UtcNow,
                    updatedAtUtc: UtcNow)
            };
            var unitOfWork = new FakeUnitOfWork();
            AdvanceCityPopulationCommandHandler handler = CreateHandler(
                archiveStateRepository: archiveStateRepository,
                unitOfWork: unitOfWork);

            AdvanceCityPopulationResult result = await handler.Handle(
                request: CreateCommand(
                    cityId: cityId,
                    tickId: 11),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: AdvanceCityPopulationStatus.CityArchived,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: result.AffectedPeopleCount);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: archiveStateRepository.RequestedCityId);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.ExecuteTransactionCalls);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        private static AdvanceCityPopulationCommand CreateCommand(
            Guid cityId,
            long tickId)
        {
            return new AdvanceCityPopulationCommand(
                CityId: cityId,
                FromSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 5,
                    hour: 9,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                ToSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 9,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                TickId: tickId);
        }

        private static AdvanceCityPopulationCommandHandler CreateHandler(
            FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
            FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
            FakeCityPopulationProgressionStateRepository? progressionStateRepository = null,
            FakeUnitOfWork? unitOfWork = null)
        {
            var householdLivelihoodPolicy = new CityHouseholdLivelihoodPolicy();
            var householdCashflowPolicy = new CityHouseholdCashflowPolicy();
            var householdEconomyPolicy = new CityHouseholdEconomyPolicy(
                householdLivelihoodPolicy: householdLivelihoodPolicy,
                householdCashflowPolicy: householdCashflowPolicy);
            var anchorSelectionPolicy = new CityPopulationAnchorSelectionPolicy();

            return new AdvanceCityPopulationCommandHandler(
                personReadRepository: new FakeCityPopulationPersonReadRepository(),
                cityPopulationArchiveStateRepository: archiveStateRepository ??
                                                      new FakeCityPopulationArchiveStateRepository(),
                cityPopulationAnchorCatalogRepository: new FakeCityPopulationAnchorCatalogRepository(),
                cityPopulationCostOfLivingStateRepository: new FakeCityPopulationCostOfLivingStateRepository(),
                cityPopulationEssentialsStateRepository: new FakeCityPopulationEssentialsStateRepository(),
                cityPopulationServiceQualityStateRepository: new FakeCityPopulationServiceQualityStateRepository(),
                cityPopulationDeletionStateRepository: deletionStateRepository ??
                                                       new FakeCityPopulationDeletionStateRepository(),
                employerFinancialStressStateRepository: new FakeCityPopulationEmployerFinancialStressStateRepository(),
                cityPopulationEnvironmentRepository: new FakeCityPopulationEnvironmentRepository(),
                householdFinancialStressStateRepository:
                new FakeCityPopulationHouseholdFinancialStressStateRepository(),
                cityPopulationLivingConditionsStateRepository: new FakeCityPopulationLivingConditionsStateRepository(),
                districtUtilityConditionsClient: new FakeCityDistrictUtilityConditionsClient(),
                commuteRoutingService: new FakeCityPopulationCommuteRoutingService(),
                commuteTripSyncService: new FakeCityPopulationCommuteTripSyncService(),
                cityPopulationActivityJournalService: new FakeCityPopulationActivityJournalService(),
                cityEconomySettlementOutboxWriter: new FakeCityEconomySettlementOutboxWriter(),
                residentFactsOutboxWriter: new FakePopulationResidentFactsOutboxWriter(),
                residentMedicalStateOutboxWriter: new FakePopulationResidentMedicalStateOutboxWriter(),
                progressionStateRepository: progressionStateRepository ??
                                            new FakeCityPopulationProgressionStateRepository(),
                cityPopulationSummaryProjectionService: new FakeCityPopulationSummaryProjectionService(),
                weatherExposureStateRepository: new FakeCityPopulationWeatherExposureStateRepository(),
                householdWriteRepository: new FakeHouseholdWriteRepository(),
                marriageDomainService: new MarriageDomainService(),
                populationBirthDomainService: new PopulationBirthDomainService(),
                personWriteRepository: new FakePersonWriteRepository(),
                birthAutonomyPolicy: new CityBirthAutonomyPolicy(
                    contentCatalog: new TestPopulationGenerationContentCatalog(),
                    householdLivelihoodPolicy: householdLivelihoodPolicy),
                civilRegistryAutonomyPolicy: new CityCivilRegistryAutonomyPolicy(),
                educationAutonomyPolicy: new CityEducationAutonomyPolicy(anchorSelectionPolicy),
                employmentAutonomyPolicy: new CityEmploymentAutonomyPolicy(
                    contentCatalog: new TestPopulationGenerationContentCatalog(),
                    householdEconomyPolicy: householdEconomyPolicy,
                    anchorSelectionPolicy: anchorSelectionPolicy),
                healthcareAutonomyPolicy: new CityHealthcareAutonomyPolicy(householdLivelihoodPolicy),
                householdCashflowPolicy: householdCashflowPolicy,
                householdPressurePolicy: new CityHouseholdPressurePolicy(),
                housingAutonomyPolicy: new CityHousingAutonomyPolicy(householdEconomyPolicy),
                householdIndependenceAutonomyPolicy: new CityHouseholdIndependenceAutonomyPolicy(
                    householdLivelihoodPolicy),
                anchorSelectionPolicy: anchorSelectionPolicy,
                districtImpactPolicy: new CityPopulationDistrictImpactPolicy(),
                healthcarePressurePolicy: new CityPopulationHealthcarePressurePolicy(),
                illnessAutonomyPolicy: new CityIllnessAutonomyPolicy(),
                livingConditionsPressurePolicy: new CityPopulationLivingConditionsPressurePolicy(),
                participationPolicy: new CityPopulationParticipationPolicy(),
                personNeedsProgressionPolicy: new PersonNeedsProgressionPolicy(),
                weatherExposurePolicy: new CityPopulationWeatherExposurePolicy(
                    new CityPopulationClimateAdaptationPolicy()),
                timeProvider: CreateTimeProvider(),
                logger: NullLogger<AdvanceCityPopulationCommandHandler>.Instance,
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
        }

        private sealed class TestPopulationGenerationContentCatalog : IPopulationGenerationContentCatalog
        {
            public IReadOnlyList<string> MaleFirstNames => ["Ivan"];
            public IReadOnlyList<string> FemaleFirstNames => ["Anna"];

            public IReadOnlyList<PopulationFamilySurnameCatalogItem> FamilySurnames =>
            [
                new(
                    Masculine: "Ivanov",
                    Feminine: "Ivanova")
            ];

            public IReadOnlyList<PopulationProfessionCatalogItem> Professions =>
            [
                new(
                    Title: "Engineer",
                    Weight: 1)
            ];
        }
    }
}
