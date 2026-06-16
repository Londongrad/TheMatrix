using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetLedger;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.RecordCityBudgetRevenue;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.BudgetOperations.RecordCityBudgetRevenue
{
    public sealed class RecordCityBudgetRevenueCommandHandlerTests
    {
        [Fact]
        public async Task Handle_CreatesBudgetRecordsRevenueAndPublishesPressureSignal()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var budgetRepository = new FakeCityBudgetRepository();
            var ledgerRepository = new FakeCityBudgetLedgerRepository();
            var unitOfWork = new FakeEconomyUnitOfWork();
            var signalPublisher = new FakeCityOperationalBudgetSignalPublisher();
            var pressureProjectionService = new FakeCityOperationalBudgetPressureProjectionService();
            var timeProvider = new FrozenTimeProvider(
                new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 8,
                    hour: 13,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero));
            var handler = new RecordCityBudgetRevenueCommandHandler(
                budgetRepository: budgetRepository,
                ledgerRepository: ledgerRepository,
                unitOfWork: unitOfWork,
                operationalBudgetSignalPublisher: signalPublisher,
                pressureProjectionService: pressureProjectionService,
                timeProvider: timeProvider);
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

            BudgetLedgerEntryDto result = await handler.Handle(
                request: command,
                cancellationToken: CancellationToken.None);

            CityBudget budget = Assert.Single(budgetRepository.AddedBudgets);
            CityBudgetLedgerEntry entry = Assert.Single(ledgerRepository.AddedEntries);
            FakeCityOperationalBudgetSignalPublisher.PublishedSignal signal =
                Assert.Single(signalPublisher.PublishedSignals);
            Assert.Equal(
                expected: cityId,
                actual: budget.CityId);
            Assert.Equal(
                expected: cityId,
                actual: pressureProjectionService.RequestedCityId);
            Assert.Equal(
                expected: timeProvider.UtcNow,
                actual: entry.OccurredAtUtc);
            Assert.Equal(
                expected: 240m,
                actual: budget.Balance.Amount);
            Assert.Equal(
                expected: 240m,
                actual: budget.TotalDirectRevenue.Amount);
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
                expected: cityId,
                actual: signal.Snapshot.CityId);
            Assert.Equal(
                expected: "Revenue",
                actual: result.Kind);
            Assert.Equal(
                expected: 240m,
                actual: result.Amount);
            Assert.Equal(
                expected: timeProvider.UtcNow.ToString("O"),
                actual: result.OccurredAtUtc);
        }
    }
}
