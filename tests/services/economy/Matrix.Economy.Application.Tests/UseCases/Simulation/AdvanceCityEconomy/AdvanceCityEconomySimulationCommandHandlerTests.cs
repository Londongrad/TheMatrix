using Matrix.Economy.Application.Scenarios.ClassicCity.Services;
using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.Economy.Application.UseCases.BudgetAllocations.Common;
using Matrix.Economy.Application.UseCases.BudgetOperations.Common;
using Matrix.Economy.Application.UseCases.Businesses.Common;
using Matrix.Economy.Application.UseCases.HouseholdObligations.Common;
using Matrix.Economy.Application.UseCases.Simulation.AdvanceCityEconomy;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Matrix.Economy.Domain.Services;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.Simulation.AdvanceCityEconomy;

public sealed class AdvanceCityEconomySimulationCommandHandlerTests
{
    private static (
        AdvanceCityEconomySimulationCommandHandler Handler,
        FakeCityEconomyProgressionStateRepository ProgressionStateRepository,
        FakeCityPopulationSignalPublisher PopulationSignalPublisher,
        FakeEconomyUnitOfWork UnitOfWork,
        FrozenTimeProvider TimeProvider) CreateSut(
        Guid cityId,
        DateTimeOffset utcNow,
        CityEconomyProgressionState? progressionState = null,
        IReadOnlyList<CityBudgetAllocation>? allocations = null,
        IReadOnlyList<CityBusiness>? businesses = null,
        IReadOnlyList<CityHouseholdObligation>? obligations = null,
        IReadOnlyList<CityHouseholdAccount>? householdAccounts = null,
        CityBudget? budget = null,
        CityEconomyCostProfileState? costProfileState = null)
    {
        var allocationRepository = new FakeCityBudgetAllocationRepository { Allocations = allocations ?? Array.Empty<CityBudgetAllocation>() };
        var budgetRepository = new FakeCityBudgetRepository { BudgetByCity = budget };
        var businessRepository = new FakeCityBusinessRepository { Businesses = businesses ?? Array.Empty<CityBusiness>() };
        var costProfileStateRepository = new FakeCityEconomyCostProfileStateRepository { StateByCity = costProfileState };
        var progressionStateRepository = new FakeCityEconomyProgressionStateRepository { StateByCity = progressionState };
        var householdAccountRepository = new FakeCityHouseholdAccountRepository { Accounts = householdAccounts ?? Array.Empty<CityHouseholdAccount>() };
        var obligationRepository = new FakeCityHouseholdObligationRepository { Obligations = obligations ?? Array.Empty<CityHouseholdObligation>() };
        var householdLedgerRepository = new FakeCityHouseholdAccountLedgerRepository();
        var businessLedgerRepository = new FakeCityBusinessLedgerRepository();
        var budgetLedgerRepository = new FakeCityBudgetLedgerRepository();
        var populationSignalPublisher = new FakeCityPopulationSignalPublisher();
        var unitOfWork = new FakeEconomyUnitOfWork();
        var timeProvider = new FrozenTimeProvider(utcNow);
        var allocationExpenseSupport = new CityBudgetAllocationExpenseSupport(
            allocationRepository,
            timeProvider);
        var chargeSupport = new HouseholdObligationChargeSupport(
            householdAccountRepository,
            householdLedgerRepository,
            businessRepository,
            businessLedgerRepository,
            timeProvider);
        var taxRemittanceSupport = new CityBusinessTaxRemittanceSupport(
            businessLedgerRepository,
            budgetRepository,
            budgetLedgerRepository,
            timeProvider);
        var disbursementSupport = new CityBudgetBusinessDisbursementSupport(
            budgetRepository,
            budgetLedgerRepository,
            businessLedgerRepository,
            allocationExpenseSupport,
            timeProvider);
        var recurringCycleExecutionService = new CityEconomyRecurringCycleExecutionService(
            allocationRepository,
            budgetRepository,
            businessRepository,
            costProfileStateRepository,
            householdAccountRepository,
            obligationRepository,
            chargeSupport,
            taxRemittanceSupport,
            new CityEconomyCostProfilePolicy(),
            new CityEconomyServiceQualityPolicy(),
            new CityMunicipalOperatingCyclePolicy(),
            disbursementSupport);
        var handler = new AdvanceCityEconomySimulationCommandHandler(
            progressionStateRepository,
            populationSignalPublisher,
            recurringCycleExecutionService,
            unitOfWork,
            timeProvider);

        return (
            handler,
            progressionStateRepository,
            populationSignalPublisher,
            unitOfWork,
            timeProvider);
    }

    [Fact]
    public async Task Handle_ReturnsDuplicateWhenTickAndDateWereAlreadyProcessed()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var existingState = CityEconomyProgressionState.Create(
            cityId: cityId,
            lastCompletedTickId: 7,
            lastProcessedDate: new DateOnly(2048, 5, 8),
            updatedAtUtc: new DateTimeOffset(2048, 5, 8, 8, 0, 0, TimeSpan.Zero));
        var sut = CreateSut(
            cityId: cityId,
            utcNow: new DateTimeOffset(2048, 5, 8, 9, 14, 0, TimeSpan.Zero),
            progressionState: existingState);

        AdvanceCityEconomySimulationResult result = await sut.Handler.Handle(
            new AdvanceCityEconomySimulationCommand(
                CityId: cityId,
                FromSimTimeUtc: new DateTimeOffset(2048, 5, 8, 0, 0, 0, TimeSpan.Zero),
                ToSimTimeUtc: new DateTimeOffset(2048, 5, 8, 12, 0, 0, TimeSpan.Zero),
                TickId: 7),
            CancellationToken.None);

        Assert.Equal(AdvanceCityEconomySimulationStatus.Duplicate, result.Status);
        Assert.Equal(0, result.ProcessedDays);
        Assert.Empty(sut.ProgressionStateRepository.AddedStates);
        Assert.Equal(0, sut.UnitOfWork.SaveChangesCallCount);
        Assert.Empty(sut.PopulationSignalPublisher.CostOfLivingSnapshots);
        Assert.Empty(sut.PopulationSignalPublisher.ServiceQualitySnapshots);
    }

    [Fact]
    public async Task Handle_ReturnsOutOfOrderWhenDateMovesBackwards()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var existingState = CityEconomyProgressionState.Create(
            cityId: cityId,
            lastCompletedTickId: 3,
            lastProcessedDate: new DateOnly(2048, 5, 10),
            updatedAtUtc: new DateTimeOffset(2048, 5, 10, 8, 0, 0, TimeSpan.Zero));
        var sut = CreateSut(
            cityId: cityId,
            utcNow: new DateTimeOffset(2048, 5, 10, 9, 52, 0, TimeSpan.Zero),
            progressionState: existingState);

        AdvanceCityEconomySimulationResult result = await sut.Handler.Handle(
            new AdvanceCityEconomySimulationCommand(
                CityId: cityId,
                FromSimTimeUtc: new DateTimeOffset(2048, 5, 8, 0, 0, 0, TimeSpan.Zero),
                ToSimTimeUtc: new DateTimeOffset(2048, 5, 9, 23, 0, 0, TimeSpan.Zero),
                TickId: 4),
            CancellationToken.None);

        Assert.Equal(AdvanceCityEconomySimulationStatus.OutOfOrder, result.Status);
        Assert.Equal(0, result.ProcessedDays);
        Assert.Empty(sut.ProgressionStateRepository.AddedStates);
        Assert.Equal(0, sut.UnitOfWork.SaveChangesCallCount);
        Assert.Empty(sut.PopulationSignalPublisher.CostOfLivingSnapshots);
        Assert.Empty(sut.PopulationSignalPublisher.ServiceQualitySnapshots);
    }

    [Fact]
    public async Task Handle_AppliesSameDayTickWithoutRunningDailyCycles()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var sut = CreateSut(
            cityId: cityId,
            utcNow: new DateTimeOffset(2048, 5, 11, 10, 31, 0, TimeSpan.Zero));

        AdvanceCityEconomySimulationResult result = await sut.Handler.Handle(
            new AdvanceCityEconomySimulationCommand(
                CityId: cityId,
                FromSimTimeUtc: new DateTimeOffset(2048, 5, 11, 8, 0, 0, TimeSpan.Zero),
                ToSimTimeUtc: new DateTimeOffset(2048, 5, 11, 18, 0, 0, TimeSpan.Zero),
                TickId: 2),
            CancellationToken.None);

        CityEconomyProgressionState state = Assert.Single(sut.ProgressionStateRepository.AddedStates);
        Assert.Equal(AdvanceCityEconomySimulationStatus.Applied, result.Status);
        Assert.Equal(0, result.ProcessedDays);
        Assert.Equal(0, result.ChargedObligations);
        Assert.Equal(0, result.RemittedBusinesses);
        Assert.Equal(0, result.MunicipalProviderPayments);
        Assert.Equal(new DateOnly(2048, 5, 11), state.LastProcessedDate);
        Assert.Equal(2, state.LastCompletedTickId);
        Assert.Equal(sut.TimeProvider.UtcNow, state.UpdatedAtUtc);
        Assert.Equal(1, sut.UnitOfWork.SaveChangesCallCount);
        Assert.Empty(sut.PopulationSignalPublisher.CostOfLivingSnapshots);
        Assert.Empty(sut.PopulationSignalPublisher.ServiceQualitySnapshots);
        Assert.Empty(sut.PopulationSignalPublisher.HouseholdFinancialStressBatches);
    }

    [Fact]
    public async Task Handle_AppliesDailyCycleAndPublishesServiceQualitySnapshot()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var sut = CreateSut(
            cityId: cityId,
            utcNow: new DateTimeOffset(2048, 5, 12, 11, 13, 0, TimeSpan.Zero));

        AdvanceCityEconomySimulationResult result = await sut.Handler.Handle(
            new AdvanceCityEconomySimulationCommand(
                CityId: cityId,
                FromSimTimeUtc: new DateTimeOffset(2048, 5, 11, 8, 0, 0, TimeSpan.Zero),
                ToSimTimeUtc: new DateTimeOffset(2048, 5, 12, 17, 30, 0, TimeSpan.Zero),
                TickId: 5),
            CancellationToken.None);

        CityEconomyProgressionState state = Assert.Single(sut.ProgressionStateRepository.AddedStates);
        ClassicCityServiceQualitySnapshotV1 snapshot = Assert.Single(sut.PopulationSignalPublisher.ServiceQualitySnapshots);
        Assert.Equal(AdvanceCityEconomySimulationStatus.Applied, result.Status);
        Assert.Equal(1, result.ProcessedDays);
        Assert.Equal(0, result.ChargedObligations);
        Assert.Equal(0, result.RemittedBusinesses);
        Assert.Equal(0, result.MunicipalProviderPayments);
        Assert.Equal(new DateOnly(2048, 5, 12), state.LastProcessedDate);
        Assert.Equal(5, state.LastCompletedTickId);
        Assert.Equal(sut.TimeProvider.UtcNow, state.UpdatedAtUtc);
        Assert.Equal(3, sut.UnitOfWork.SaveChangesCallCount);
        Assert.Equal(cityId, snapshot.CityId);
        Assert.Equal(new DateTimeOffset(2048, 5, 12, 17, 30, 0, TimeSpan.Zero), snapshot.OccurredAtUtc);
        Assert.Empty(sut.PopulationSignalPublisher.CostOfLivingSnapshots);
        Assert.Empty(sut.PopulationSignalPublisher.HouseholdFinancialStressBatches);
    }

}
