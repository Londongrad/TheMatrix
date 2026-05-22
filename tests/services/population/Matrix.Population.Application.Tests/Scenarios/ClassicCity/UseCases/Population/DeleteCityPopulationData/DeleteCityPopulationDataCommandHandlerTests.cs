using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.DeleteCityPopulationData;
using Matrix.Population.Application.Tests.TestSupport;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.DeleteCityPopulationData;

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
        var handler = CreateHandler(
            processedRepository: processedRepository,
            unitOfWork: unitOfWork);

        DeleteCityPopulationDataResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(DeleteCityPopulationDataStatus.Duplicate, result.Status);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenDeleteTimestampIsOlderThanExisting_ReturnsStale()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var deletionStateRepository = new FakeCityPopulationDeletionStateRepository
        {
            State = CityPopulationDeletionState.Create(
                cityId: CityId.From(cityId),
                deletedAtUtc: new DateTimeOffset(2048, 5, 4, 14, 30, 0, TimeSpan.Zero),
                updatedAtUtc: new DateTimeOffset(2048, 5, 4, 14, 31, 0, TimeSpan.Zero))
        };
        var householdWriteRepository = new FakeHouseholdWriteRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            deletionStateRepository: deletionStateRepository,
            householdWriteRepository: householdWriteRepository,
            unitOfWork: unitOfWork);

        DeleteCityPopulationDataResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(DeleteCityPopulationDataStatus.Stale, result.Status);
        Assert.Equal(0, householdWriteRepository.DeleteByCityCalls);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenDeletionStateDoesNotExist_DeletesCityDataAndCreatesDeletionState()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var householdWriteRepository = new FakeHouseholdWriteRepository();
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
        var householdFinancialStressStateRepository = new FakeCityPopulationHouseholdFinancialStressStateRepository();
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
        var activityJournalService = new FakeCityPopulationActivityJournalService();
        var summaryProjectionService = new FakeCityPopulationSummaryProjectionService();
        var weatherImpactStateRepository = new FakeCityPopulationWeatherImpactStateRepository();
        var weatherExposureStateRepository = new FakeCityPopulationWeatherExposureStateRepository();
        var deletionStateRepository = new FakeCityPopulationDeletionStateRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            householdWriteRepository: householdWriteRepository,
            archiveStateRepository: archiveStateRepository,
            costOfLivingStateRepository: costOfLivingStateRepository,
            essentialsStateRepository: essentialsStateRepository,
            employerFinancialStressStateRepository: employerFinancialStressStateRepository,
            environmentRepository: environmentRepository,
            householdFinancialStressStateRepository: householdFinancialStressStateRepository,
            livingConditionsStateRepository: livingConditionsStateRepository,
            progressionStateRepository: progressionStateRepository,
            serviceQualityStateRepository: serviceQualityStateRepository,
            activityJournalService: activityJournalService,
            summaryProjectionService: summaryProjectionService,
            weatherImpactStateRepository: weatherImpactStateRepository,
            weatherExposureStateRepository: weatherExposureStateRepository,
            deletionStateRepository: deletionStateRepository,
            unitOfWork: unitOfWork);

        DeleteCityPopulationDataResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(DeleteCityPopulationDataStatus.Applied, result.Status);
        Assert.Equal(1, householdWriteRepository.DeleteByCityCalls);
        Assert.Equal(1, archiveStateRepository.DeleteByCityCalls);
        Assert.Equal(1, costOfLivingStateRepository.DeleteByCityCalls);
        Assert.Equal(1, essentialsStateRepository.DeleteByCityCalls);
        Assert.Equal(1, employerFinancialStressStateRepository.DeleteByCityCalls);
        Assert.Equal(1, environmentRepository.DeleteByCityCalls);
        Assert.Equal(1, householdFinancialStressStateRepository.DeleteByCityCalls);
        Assert.Equal(1, livingConditionsStateRepository.DeleteByCityCalls);
        Assert.Equal(1, progressionStateRepository.DeleteByCityCalls);
        Assert.Equal(1, serviceQualityStateRepository.DeleteByCityCalls);
        Assert.Equal(1, weatherImpactStateRepository.DeleteByCityCalls);
        Assert.Equal(1, weatherExposureStateRepository.DeleteByCityCalls);
        Assert.Equal(1, activityJournalService.DeleteByCityCalls);
        Assert.Equal(CityId.From(cityId), Assert.Single(summaryProjectionService.DeletedCityIds));
        CityPopulationDeletionState deletionState = Assert.Single(deletionStateRepository.AddedStates);
        Assert.Equal(new DateTimeOffset(2048, 5, 4, 14, 0, 0, TimeSpan.Zero), deletionState.DeletedAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task Handle_WhenDeletionStateExists_UpdatesDeletionTimestamp()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityPopulationDeletionState existingState = CityPopulationDeletionState.Create(
            cityId: CityId.From(cityId),
            deletedAtUtc: new DateTimeOffset(2048, 5, 4, 12, 0, 0, TimeSpan.Zero),
            updatedAtUtc: new DateTimeOffset(2048, 5, 4, 12, 1, 0, TimeSpan.Zero));
        var deletionStateRepository = new FakeCityPopulationDeletionStateRepository
        {
            State = existingState
        };
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(
            deletionStateRepository: deletionStateRepository,
            unitOfWork: unitOfWork);

        DeleteCityPopulationDataResult result = await handler.Handle(CreateCommand(), CancellationToken.None);

        Assert.Equal(DeleteCityPopulationDataStatus.Applied, result.Status);
        Assert.Empty(deletionStateRepository.AddedStates);
        Assert.Equal(new DateTimeOffset(2048, 5, 4, 14, 0, 0, TimeSpan.Zero), existingState.DeletedAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    private static DeleteCityPopulationDataCommandHandler CreateHandler(
        FakeHouseholdWriteRepository? householdWriteRepository = null,
        FakeCityPopulationArchiveStateRepository? archiveStateRepository = null,
        FakeCityPopulationCostOfLivingStateRepository? costOfLivingStateRepository = null,
        FakeCityPopulationEssentialsStateRepository? essentialsStateRepository = null,
        FakeCityPopulationEmployerFinancialStressStateRepository? employerFinancialStressStateRepository = null,
        FakeCityPopulationEnvironmentRepository? environmentRepository = null,
        FakeCityPopulationHouseholdFinancialStressStateRepository? householdFinancialStressStateRepository = null,
        FakeCityPopulationLivingConditionsStateRepository? livingConditionsStateRepository = null,
        FakeCityPopulationProgressionStateRepository? progressionStateRepository = null,
        FakeCityPopulationServiceQualityStateRepository? serviceQualityStateRepository = null,
        FakeCityPopulationActivityJournalService? activityJournalService = null,
        FakeCityPopulationSummaryProjectionService? summaryProjectionService = null,
        FakeCityPopulationWeatherImpactStateRepository? weatherImpactStateRepository = null,
        FakeCityPopulationWeatherExposureStateRepository? weatherExposureStateRepository = null,
        FakeCityPopulationDeletionStateRepository? deletionStateRepository = null,
        FakeProcessedIntegrationMessageRepository? processedRepository = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new DeleteCityPopulationDataCommandHandler(
            householdWriteRepository ?? new FakeHouseholdWriteRepository(),
            archiveStateRepository ?? new FakeCityPopulationArchiveStateRepository(),
            costOfLivingStateRepository ?? new FakeCityPopulationCostOfLivingStateRepository(),
            essentialsStateRepository ?? new FakeCityPopulationEssentialsStateRepository(),
            employerFinancialStressStateRepository ?? new FakeCityPopulationEmployerFinancialStressStateRepository(),
            environmentRepository ?? new FakeCityPopulationEnvironmentRepository(),
            householdFinancialStressStateRepository ?? new FakeCityPopulationHouseholdFinancialStressStateRepository(),
            livingConditionsStateRepository ?? new FakeCityPopulationLivingConditionsStateRepository(),
            progressionStateRepository ?? new FakeCityPopulationProgressionStateRepository(),
            serviceQualityStateRepository ?? new FakeCityPopulationServiceQualityStateRepository(),
            activityJournalService ?? new FakeCityPopulationActivityJournalService(),
            summaryProjectionService ?? new FakeCityPopulationSummaryProjectionService(),
            weatherImpactStateRepository ?? new FakeCityPopulationWeatherImpactStateRepository(),
            weatherExposureStateRepository ?? new FakeCityPopulationWeatherExposureStateRepository(),
            deletionStateRepository ?? new FakeCityPopulationDeletionStateRepository(),
            processedRepository ?? new FakeProcessedIntegrationMessageRepository(),
            CreateTimeProvider(),
            unitOfWork ?? new FakeUnitOfWork());
    }

    private static DeleteCityPopulationDataCommand CreateCommand()
    {
        return new DeleteCityPopulationDataCommand(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            IntegrationMessageId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            ConsumerName: "population-delete",
            DeletedAtUtc: new DateTimeOffset(2048, 5, 4, 14, 0, 0, TimeSpan.Zero));
    }
}
