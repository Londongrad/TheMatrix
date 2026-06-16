using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetLedger;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.RecordCityBudgetExpense;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.BudgetOperations.RecordCityBudgetExpense
{
    public sealed class RecordCityBudgetExpenseCommandHandlerTests
    {
        [Fact]
        public async Task Handle_RecordsExpenseUpdatesAllocationAndPublishesPressureSignal()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityBudget budget = CreateBudget(cityId);
            budget.ApplyLedgerEntry(
                CreateBudgetEntry(
                    cityId: cityId,
                    kind: CityBudgetLedgerEntryKind.Revenue,
                    amount: 500m,
                    title: "Opening Revenue"));
            CityBudgetAllocation allocation = CreateAllocation(
                cityId: cityId,
                category: CityBudgetCategory.Infrastructure,
                targetAmount: 300m,
                spentAmount: 20m);
            var budgetRepository = new FakeCityBudgetRepository
            {
                BudgetByCity = budget
            };
            var allocationRepository = new FakeCityBudgetAllocationRepository
            {
                Allocations = [allocation]
            };
            var ledgerRepository = new FakeCityBudgetLedgerRepository();
            var unitOfWork = new FakeEconomyUnitOfWork();
            var signalPublisher = new FakeCityOperationalBudgetSignalPublisher();
            var pressureProjectionService = new FakeCityOperationalBudgetPressureProjectionService();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 8,
                    hour: 14,
                    minute: 15,
                    second: 0,
                    offset: TimeSpan.Zero));
            var allocationExpenseSupport = new CityBudgetAllocationExpenseSupport(
                allocationRepository: allocationRepository,
                timeProvider: timeProvider);
            var handler = new RecordCityBudgetExpenseCommandHandler(
                budgetRepository: budgetRepository,
                ledgerRepository: ledgerRepository,
                allocationExpenseSupport: allocationExpenseSupport,
                unitOfWork: unitOfWork,
                operationalBudgetSignalPublisher: signalPublisher,
                pressureProjectionService: pressureProjectionService,
                timeProvider: timeProvider);
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

            BudgetLedgerEntryDto result = await handler.Handle(
                request: command,
                cancellationToken: CancellationToken.None);

            CityBudgetLedgerEntry entry = Assert.Single(ledgerRepository.AddedEntries);
            FakeCityOperationalBudgetSignalPublisher.PublishedSignal signal =
                Assert.Single(signalPublisher.PublishedSignals);
            Assert.Equal(
                expected: cityId,
                actual: allocationRepository.RequestedCityId);
            Assert.Equal(
                expected: cityId,
                actual: pressureProjectionService.RequestedCityId);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: entry.OccurredAtUtc);
            Assert.Equal(
                expected: 420m,
                actual: budget.Balance.Amount);
            Assert.Equal(
                expected: 80m,
                actual: budget.TotalCityExpenses.Amount);
            Assert.Equal(
                expected: 100m,
                actual: allocation.TotalSpent.Amount);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: allocation.UpdatedAtUtc);
            Assert.Equal(
                expected: 2,
                actual: unitOfWork.SaveChangesCallCount);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: signal.EffectiveAtUtc);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: signal.OccurredAtUtc);
            Assert.Equal(
                expected: "Expense",
                actual: result.Kind);
            Assert.Equal(
                expected: 80m,
                actual: result.Amount);
            Assert.Equal(
                expected: timeProvider.UtcNow.ToString("O"),
                actual: result.OccurredAtUtc);
        }
    }
}
