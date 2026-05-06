using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.BudgetAllocations.Common;
using Matrix.Economy.Application.UseCases.BudgetOperations.RecordCityBudgetExpense;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.BudgetOperations.RecordCityBudgetExpense;

public sealed class RecordCityBudgetExpenseCommandHandlerTests
{
    [Fact]
    public async Task Handle_RecordsExpenseUpdatesAllocationAndPublishesPressureSignal()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        CityBudget budget = CreateBudget(cityId);
        budget.ApplyLedgerEntry(CreateBudgetEntry(cityId, CityBudgetLedgerEntryKind.Revenue, 500m, "Opening Revenue"));
        var allocation = CreateAllocation(cityId, CityBudgetCategory.Infrastructure, 300m, spentAmount: 20m);
        var budgetRepository = new FakeCityBudgetRepository { BudgetByCity = budget };
        var allocationRepository = new FakeCityBudgetAllocationRepository { Allocations = [allocation] };
        var ledgerRepository = new FakeCityBudgetLedgerRepository();
        var unitOfWork = new FakeEconomyUnitOfWork();
        var signalPublisher = new FakeCityOperationalBudgetSignalPublisher();
        var pressureProjectionService = new FakeCityOperationalBudgetPressureProjectionService();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 8, 14, 15, 0, TimeSpan.Zero));
        var allocationExpenseSupport = new CityBudgetAllocationExpenseSupport(
            allocationRepository,
            timeProvider);
        var handler = new RecordCityBudgetExpenseCommandHandler(
            budgetRepository,
            ledgerRepository,
            allocationExpenseSupport,
            unitOfWork,
            signalPublisher,
            pressureProjectionService,
            timeProvider);
        var command = new RecordCityBudgetExpenseCommand(
            CityId: cityId,
            Category: CityBudgetCategory.Infrastructure,
            Amount: 80m,
            Title: "Road Repair",
            Description: "Bridge maintenance",
            UnitKind: null,
            UnitCode: null,
            UnitDisplayName: null,
            UnitSymbol: null);

        var result = await handler.Handle(command, CancellationToken.None);

        var entry = Assert.Single(ledgerRepository.AddedEntries);
        var signal = Assert.Single(signalPublisher.PublishedSignals);
        Assert.Equal(cityId, allocationRepository.RequestedCityId);
        Assert.Equal(cityId, pressureProjectionService.RequestedCityId);
        Assert.Equal(timeProvider.UtcNow, entry.OccurredAtUtc);
        Assert.Equal(420m, budget.Balance.Amount);
        Assert.Equal(80m, budget.TotalCityExpenses.Amount);
        Assert.Equal(100m, allocation.TotalSpent.Amount);
        Assert.Equal(timeProvider.UtcNow, allocation.UpdatedAtUtc);
        Assert.Equal(2, unitOfWork.SaveChangesCallCount);
        Assert.Equal(timeProvider.UtcNow, signal.EffectiveAtUtc);
        Assert.Equal(timeProvider.UtcNow, signal.OccurredAtUtc);
        Assert.Equal("Expense", result.Kind);
        Assert.Equal(80m, result.Amount);
        Assert.Equal(timeProvider.UtcNow.ToString("O"), result.OccurredAtUtc);
    }
}
