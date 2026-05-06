using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Application.UseCases.BudgetOperations.RecordCityBudgetRevenue;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.BudgetOperations.RecordCityBudgetRevenue;

public sealed class RecordCityBudgetRevenueCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesBudgetRecordsRevenueAndPublishesPressureSignal()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var budgetRepository = new FakeCityBudgetRepository();
        var ledgerRepository = new FakeCityBudgetLedgerRepository();
        var unitOfWork = new FakeEconomyUnitOfWork();
        var signalPublisher = new FakeCityOperationalBudgetSignalPublisher();
        var pressureProjectionService = new FakeCityOperationalBudgetPressureProjectionService();
        var timeProvider = new FrozenTimeProvider(new DateTimeOffset(2048, 5, 8, 13, 30, 0, TimeSpan.Zero));
        var handler = new RecordCityBudgetRevenueCommandHandler(
            budgetRepository,
            ledgerRepository,
            unitOfWork,
            signalPublisher,
            pressureProjectionService,
            timeProvider);
        var command = new RecordCityBudgetRevenueCommand(
            CityId: cityId,
            Category: CityBudgetCategory.General,
            Amount: 240m,
            Title: "Grant Revenue",
            Description: "Regional support",
            UnitKind: null,
            UnitCode: null,
            UnitDisplayName: null,
            UnitSymbol: null);

        var result = await handler.Handle(command, CancellationToken.None);

        var budget = Assert.Single(budgetRepository.AddedBudgets);
        var entry = Assert.Single(ledgerRepository.AddedEntries);
        var signal = Assert.Single(signalPublisher.PublishedSignals);
        Assert.Equal(cityId, budget.CityId);
        Assert.Equal(cityId, pressureProjectionService.RequestedCityId);
        Assert.Equal(timeProvider.UtcNow, entry.OccurredAtUtc);
        Assert.Equal(240m, budget.Balance.Amount);
        Assert.Equal(240m, budget.TotalDirectRevenue.Amount);
        Assert.Equal(2, unitOfWork.SaveChangesCallCount);
        Assert.Equal(timeProvider.UtcNow, signal.EffectiveAtUtc);
        Assert.Equal(timeProvider.UtcNow, signal.OccurredAtUtc);
        Assert.Equal(cityId, signal.Snapshot.CityId);
        Assert.Equal("Revenue", result.Kind);
        Assert.Equal(240m, result.Amount);
        Assert.Equal(timeProvider.UtcNow.ToString("O"), result.OccurredAtUtc);
    }
}
