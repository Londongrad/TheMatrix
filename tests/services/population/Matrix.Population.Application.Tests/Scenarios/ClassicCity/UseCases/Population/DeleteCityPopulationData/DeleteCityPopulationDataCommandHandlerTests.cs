using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.DeleteCityPopulationData;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.DeleteCityPopulationData
{
    public sealed class DeleteCityPopulationDataCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenMessageAlreadyProcessed_ReturnsDuplicate()
        {
            var processedRepository = new FakeProcessedIntegrationMessageRepository
            {
                TryMarkProcessedResult = false
            };
            var unitOfWork = new FakeUnitOfWork();
            DeleteCityPopulationDataCommandHandler handler = CreateHandler(
                processedRepository: processedRepository,
                unitOfWork: unitOfWork);

            DeleteCityPopulationDataResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: DeleteCityPopulationDataStatus.Duplicate,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenDeleteTimestampIsOlderThanExisting_ReturnsStale()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var deletionStateRepository = new FakeCityPopulationDeletionStateRepository
            {
                State = CityPopulationDeletionState.Create(
                    cityId: CityId.From(cityId),
                    deletedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 4,
                        hour: 14,
                        minute: 30,
                        second: 0,
                        offset: TimeSpan.Zero),
                    updatedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 4,
                        hour: 14,
                        minute: 31,
                        second: 0,
                        offset: TimeSpan.Zero))
            };
            var householdWriteRepository = new FakeHouseholdWriteRepository();
            var educationParticipationProjectionRepository =
                new FakeEducationParticipationProjectionRepository();
            var unitOfWork = new FakeUnitOfWork();
            DeleteCityPopulationDataCommandHandler handler = CreateHandler(
                deletionStateRepository: deletionStateRepository,
                householdWriteRepository: householdWriteRepository,
                educationParticipationProjectionRepository: educationParticipationProjectionRepository,
                unitOfWork: unitOfWork);

            DeleteCityPopulationDataResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: DeleteCityPopulationDataStatus.Stale,
                actual: result.Status);
            Assert.Equal(
                expected: 0,
                actual: householdWriteRepository.DeleteByCityCalls);
            Assert.Empty(
                educationParticipationProjectionRepository.DeletedSimulationHostIds);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenDeletionStateDoesNotExist_DeletesCityDataAndCreatesDeletionState()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var householdWriteRepository = new FakeHouseholdWriteRepository();
            var educationParticipationProjectionRepository =
                new FakeEducationParticipationProjectionRepository();
            var archiveStateRepository = new FakeCityPopulationArchiveStateRepository
            {
                State = CityPopulationArchiveState.Create(
                    cityId: CityId.From(cityId),
                    archivedAtUtc: UtcNow.AddDays(-3),
                    updatedAtUtc: UtcNow.AddDays(-2))
            };
            var costOfLivingStateRepository = new FakeCityPopulationCostOfLivingStateRepository
            {
                State = CityPopulationCostOfLivingState.Create(
                    cityId: CityId.From(cityId),
                    wageMultiplier: 1.1m,
                    retailPriceMultiplier: 1.02m,
                    housingCostMultiplier: 1.15m,
                    utilityCostMultiplier: 1.08m,
                    costOfLivingIndex: 1.12m,
                    affordabilityIndex: 0.94m,
                    lastEvaluatedAtUtc: UtcNow.AddDays(-1),
                    updatedAtUtc: UtcNow)
            };
            var essentialsStateRepository = new FakeCityPopulationEssentialsStateRepository();
            var employerFinancialStressStateRepository = new FakeCityPopulationEmployerFinancialStressStateRepository();
            employerFinancialStressStateRepository.States.Add(
                CityPopulationEmployerFinancialStressState.Create(
                    cityId: CityId.From(cityId),
                    workplaceId: WorkplaceId.From(Guid.Parse("11111111-1111-1111-1111-111111111111")),
                    requestedGrossPayrollAmount: 5000m,
                    paidGrossPayrollAmount: 4500m,
                    missedGrossPayrollAmount: 500m,
                    payrollFulfillmentRatio: 0.90m,
                    failedPayrollCount: 1,
                    partialPayrollCount: 0,
                    currentBalanceAmount: -50m,
                    distressScore: 0.40m,
                    hasHiringFreeze: true,
                    hasLayoffPressure: false,
                    lastEvaluatedAtUtc: UtcNow.AddDays(-1),
                    updatedAtUtc: UtcNow));
            var environmentRepository = new FakeCityPopulationEnvironmentRepository();
            var householdFinancialStressStateRepository =
                new FakeCityPopulationHouseholdFinancialStressStateRepository();
            householdFinancialStressStateRepository.States.Add(
                CityPopulationHouseholdFinancialStressState.Create(
                    cityId: CityId.From(cityId),
                    householdId: HouseholdId.From(Guid.Parse("22222222-2222-2222-2222-222222222222")),
                    overdueObligationCount: 1,
                    overdueRentCount: 0,
                    overdueUtilityCount: 1,
                    arrearsObligationCount: 0,
                    serviceCutoffCount: 0,
                    evictionNoticeCount: 0,
                    evictionEligibleCount: 0,
                    oldestOverdueAgeDays: 10,
                    totalOverdueAmount: 250m,
                    distressScore: 0.33m,
                    lastEvaluatedAtUtc: UtcNow.AddDays(-1),
                    updatedAtUtc: UtcNow));
            var livingConditionsStateRepository = new FakeCityPopulationLivingConditionsStateRepository();
            var progressionStateRepository = new FakeCityPopulationProgressionStateRepository();
            var serviceQualityStateRepository = new FakeCityPopulationServiceQualityStateRepository
            {
                State = CityPopulationServiceQualityState.Create(
                    cityId: CityId.From(cityId),
                    healthcareQualityIndex: 1.02m,
                    educationQualityIndex: 0.97m,
                    housingSupportIndex: 1.01m,
                    lastEvaluatedAtUtc: UtcNow.AddDays(-1),
                    updatedAtUtc: UtcNow)
            };
            var healthcarePressureSnapshotRepository =
                new FakeCityHealthcarePressureSnapshotRepository();
            var activityJournalService = new FakeCityPopulationActivityJournalService();
            var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
            var pendingWeatherImpactRepository = new FakeCityPopulationPendingWeatherImpactRepository();
            var weatherImpactStateRepository = new FakeCityPopulationWeatherImpactStateRepository();
            var weatherExposureStateRepository = new FakeCityPopulationWeatherExposureStateRepository();
            var deletionStateRepository = new FakeCityPopulationDeletionStateRepository();
            var unitOfWork = new FakeUnitOfWork();
            DeleteCityPopulationDataCommandHandler handler = CreateHandler(
                householdWriteRepository: householdWriteRepository,
                educationParticipationProjectionRepository: educationParticipationProjectionRepository,
                archiveStateRepository: archiveStateRepository,
                costOfLivingStateRepository: costOfLivingStateRepository,
                essentialsStateRepository: essentialsStateRepository,
                employerFinancialStressStateRepository: employerFinancialStressStateRepository,
                environmentRepository: environmentRepository,
                householdFinancialStressStateRepository: householdFinancialStressStateRepository,
                livingConditionsStateRepository: livingConditionsStateRepository,
                progressionStateRepository: progressionStateRepository,
                serviceQualityStateRepository: serviceQualityStateRepository,
                healthcarePressureSnapshotRepository: healthcarePressureSnapshotRepository,
                activityJournalService: activityJournalService,
                summaryProjectionService: summaryProjectionService,
                pendingWeatherImpactRepository: pendingWeatherImpactRepository,
                weatherImpactStateRepository: weatherImpactStateRepository,
                weatherExposureStateRepository: weatherExposureStateRepository,
                deletionStateRepository: deletionStateRepository,
                unitOfWork: unitOfWork);

            DeleteCityPopulationDataResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: DeleteCityPopulationDataStatus.Applied,
                actual: result.Status);
            Assert.Equal(
                expected: 1,
                actual: householdWriteRepository.DeleteByCityCalls);
            Assert.Equal(
                expected: cityId,
                actual: Assert.Single(
                    educationParticipationProjectionRepository.DeletedSimulationHostIds));
            Assert.Equal(
                expected: 1,
                actual: archiveStateRepository.DeleteByCityCalls);
            Assert.Equal(
                expected: 1,
                actual: costOfLivingStateRepository.DeleteByCityCalls);
            Assert.Equal(
                expected: 1,
                actual: essentialsStateRepository.DeleteByCityCalls);
            Assert.Equal(
                expected: 1,
                actual: employerFinancialStressStateRepository.DeleteByCityCalls);
            Assert.Equal(
                expected: 1,
                actual: environmentRepository.DeleteByCityCalls);
            Assert.Equal(
                expected: 1,
                actual: householdFinancialStressStateRepository.DeleteByCityCalls);
            Assert.Equal(
                expected: 1,
                actual: livingConditionsStateRepository.DeleteByCityCalls);
            Assert.Equal(
                expected: 1,
                actual: progressionStateRepository.DeleteByCityCalls);
            Assert.Equal(
                expected: 1,
                actual: serviceQualityStateRepository.DeleteByCityCalls);
            Assert.Equal(
                expected: 1,
                actual: healthcarePressureSnapshotRepository.DeleteByCityCalls);
            Assert.Equal(
                expected: 1,
                actual: pendingWeatherImpactRepository.DeleteByCityCalls);
            Assert.Equal(
                expected: 1,
                actual: weatherImpactStateRepository.DeleteByCityCalls);
            Assert.Equal(
                expected: 1,
                actual: weatherExposureStateRepository.DeleteByCityCalls);
            Assert.Equal(
                expected: 1,
                actual: activityJournalService.DeleteByCityCalls);
            Assert.Equal(
                expected: CityId.From(cityId),
                actual: Assert.Single(summaryProjectionService.DeletedCityIds));
            CityPopulationDeletionState deletionState = Assert.Single(deletionStateRepository.AddedStates);
            Assert.Equal(
                expected: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 4,
                    hour: 14,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                actual: deletionState.DeletedAtUtc);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        [Fact]
        public async Task Handle_WhenDeletionStateExists_UpdatesDeletionTimestamp()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var existingState = CityPopulationDeletionState.Create(
                cityId: CityId.From(cityId),
                deletedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 4,
                    hour: 12,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                updatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 4,
                    hour: 12,
                    minute: 1,
                    second: 0,
                    offset: TimeSpan.Zero));
            var deletionStateRepository = new FakeCityPopulationDeletionStateRepository
            {
                State = existingState
            };
            var unitOfWork = new FakeUnitOfWork();
            DeleteCityPopulationDataCommandHandler handler = CreateHandler(
                deletionStateRepository: deletionStateRepository,
                unitOfWork: unitOfWork);

            DeleteCityPopulationDataResult result = await handler.Handle(
                request: CreateCommand(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: DeleteCityPopulationDataStatus.Applied,
                actual: result.Status);
            Assert.Empty(deletionStateRepository.AddedStates);
            Assert.Equal(
                expected: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 4,
                    hour: 14,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                actual: existingState.DeletedAtUtc);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
        }

        private static DeleteCityPopulationDataCommandHandler CreateHandler(
            FakeHouseholdWriteRepository? householdWriteRepository = null,
            FakeEducationParticipationProjectionRepository? educationParticipationProjectionRepository = null,
            FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
            FakeCityPopulationCostOfLivingStateRepository? costOfLivingStateRepository = null,
            FakeCityPopulationEssentialsStateRepository? essentialsStateRepository = null,
            FakeCityPopulationEmployerFinancialStressStateRepository? employerFinancialStressStateRepository = null,
            FakeCityPopulationEnvironmentRepository? environmentRepository = null,
            FakeCityPopulationHouseholdFinancialStressStateRepository? householdFinancialStressStateRepository = null,
            FakeCityPopulationLivingConditionsStateRepository? livingConditionsStateRepository = null,
            FakeCityPopulationProgressionStateRepository? progressionStateRepository = null,
            FakeCityPopulationServiceQualityStateRepository? serviceQualityStateRepository = null,
            FakeCityHealthcarePressureSnapshotRepository? healthcarePressureSnapshotRepository = null,
            FakeCityPopulationActivityJournalService? activityJournalService = null,
            FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
            FakeCityPopulationPendingWeatherImpactRepository? pendingWeatherImpactRepository = null,
            FakeCityPopulationWeatherImpactStateRepository? weatherImpactStateRepository = null,
            FakeCityPopulationWeatherExposureStateRepository? weatherExposureStateRepository = null,
            FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
            FakeProcessedIntegrationMessageRepository? processedRepository = null,
            FakeUnitOfWork? unitOfWork = null)
        {
            return new DeleteCityPopulationDataCommandHandler(
                householdWriteRepository: householdWriteRepository ?? new FakeHouseholdWriteRepository(),
                educationParticipationProjectionRepository: educationParticipationProjectionRepository ??
                                                            new FakeEducationParticipationProjectionRepository(),
                cityPopulationArchiveStateRepository: archiveStateRepository ??
                                                      new FakeCityPopulationArchiveStateRepository(),
                cityPopulationCostOfLivingStateRepository: costOfLivingStateRepository ??
                                                           new FakeCityPopulationCostOfLivingStateRepository(),
                cityPopulationEssentialsStateRepository: essentialsStateRepository ??
                                                         new FakeCityPopulationEssentialsStateRepository(),
                employerFinancialStressStateRepository: employerFinancialStressStateRepository ??
                                                        new FakeCityPopulationEmployerFinancialStressStateRepository(),
                cityPopulationEnvironmentRepository: environmentRepository ??
                                                     new FakeCityPopulationEnvironmentRepository(),
                householdFinancialStressStateRepository: householdFinancialStressStateRepository ??
                                                         new FakeCityPopulationHouseholdFinancialStressStateRepository(),
                cityPopulationLivingConditionsStateRepository: livingConditionsStateRepository ??
                                                               new FakeCityPopulationLivingConditionsStateRepository(),
                cityPopulationProgressionStateRepository: progressionStateRepository ??
                                                          new FakeCityPopulationProgressionStateRepository(),
                cityPopulationServiceQualityStateRepository: serviceQualityStateRepository ??
                                                             new FakeCityPopulationServiceQualityStateRepository(),
                healthcarePressureSnapshotRepository: healthcarePressureSnapshotRepository ??
                                                      new FakeCityHealthcarePressureSnapshotRepository(),
                cityPopulationActivityJournalService: activityJournalService ??
                                                      new FakeCityPopulationActivityJournalService(),
                cityPopulationSummaryProjectionService: summaryProjectionService ??
                                                        new FakeCityPopulationSummaryProjectionService(),
                pendingWeatherImpactRepository: pendingWeatherImpactRepository ??
                                                new FakeCityPopulationPendingWeatherImpactRepository(),
                cityPopulationWeatherImpactStateRepository: weatherImpactStateRepository ??
                                                            new FakeCityPopulationWeatherImpactStateRepository(),
                cityPopulationWeatherExposureStateRepository: weatherExposureStateRepository ??
                                                              new FakeCityPopulationWeatherExposureStateRepository(),
                cityPopulationDeletionStateRepository: deletionStateRepository ??
                                                       new FakeCityPopulationDeletionStateRepository(),
                processedIntegrationMessageRepository: processedRepository ??
                                                       new FakeProcessedIntegrationMessageRepository(),
                timeProvider: CreateTimeProvider(),
                unitOfWork: unitOfWork ?? new FakeUnitOfWork());
        }

        private static DeleteCityPopulationDataCommand CreateCommand()
        {
            return new DeleteCityPopulationDataCommand(
                CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                ConsumerName: "population-delete",
                DeletedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 4,
                    hour: 14,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }
    }
}
