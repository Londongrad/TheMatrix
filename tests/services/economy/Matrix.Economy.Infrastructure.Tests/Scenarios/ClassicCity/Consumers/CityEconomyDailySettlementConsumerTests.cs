using System.Data;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Services;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers;
using Matrix.Economy.Infrastructure.Tests.TestSupport;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Matrix.Economy.Infrastructure.Tests.Scenarios.ClassicCity.Consumers
{
    public sealed class CityEconomyDailySettlementConsumerTests
    {
        [Fact]
        public async Task ConsumeAsync_WhenSettlementAlreadyExists_LogsDebugAndSkipsMutation()
        {
            var budgetRepository = new TestCityBudgetRepository();
            var ledgerRepository = new TestCityBudgetLedgerRepository();
            var settlementRepository = new TestCityBudgetSettlementRepository
            {
                ExistsResult = true
            };
            var unitOfWork = new TestEconomyUnitOfWork();
            var logger = new TestLogger<CityEconomyDailySettlementConsumer>();
            var consumer = new CityEconomyDailySettlementConsumer(
                budgetRepository: budgetRepository,
                ledgerRepository: ledgerRepository,
                settlementRepository: settlementRepository,
                deletionRepository: new TestCityEconomyDeletionRepository(),
                unitOfWork: unitOfWork,
                operatingExpensePolicy: new CityBudgetOperatingExpensePolicy(),
                timeProvider: new FrozenTimeProvider(
                    new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 6,
                        hour: 12,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.Zero)),
                logger: logger);

            await consumer.ConsumeAsync(
                message: CreateMessage(),
                cancellationToken: CancellationToken.None);

            Assert.Empty(budgetRepository.AddedBudgets);
            Assert.Empty(ledgerRepository.Entries);
            Assert.Empty(settlementRepository.AddedSettlements);
            Assert.Equal(
                expected: 0,
                actual: unitOfWork.SaveChangesCalls);
            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Debug,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Skipped duplicate city economy settlement",
                actualString: entry.Message);
        }

        [Fact]
        public async Task ConsumeAsync_WhenSettlementIsNew_AppliesBudgetAndWritesLedgersUsingInjectedTime()
        {
            DateTimeOffset ledgerTime = new(
                year: 2048,
                month: 5,
                day: 6,
                hour: 12,
                minute: 34,
                second: 56,
                offset: TimeSpan.Zero);
            var budgetRepository = new TestCityBudgetRepository();
            var ledgerRepository = new TestCityBudgetLedgerRepository();
            var settlementRepository = new TestCityBudgetSettlementRepository();
            var unitOfWork = new TestEconomyUnitOfWork();
            var logger = new TestLogger<CityEconomyDailySettlementConsumer>();
            var consumer = new CityEconomyDailySettlementConsumer(
                budgetRepository: budgetRepository,
                ledgerRepository: ledgerRepository,
                settlementRepository: settlementRepository,
                deletionRepository: new TestCityEconomyDeletionRepository(),
                unitOfWork: unitOfWork,
                operatingExpensePolicy: new CityBudgetOperatingExpensePolicy(),
                timeProvider: new FrozenTimeProvider(ledgerTime),
                logger: logger);
            CityEconomyDailySettlementV1 message = CreateMessage();

            await consumer.ConsumeAsync(
                message: message,
                cancellationToken: CancellationToken.None);

            CityBudget budget = Assert.Single(budgetRepository.AddedBudgets);
            Assert.Equal(
                expected: message.CityId,
                actual: budget.CityId);
            Assert.Equal(
                expected: 2,
                actual: unitOfWork.SaveChangesCalls);
            CityBudgetSettlement settlement = Assert.Single(settlementRepository.AddedSettlements);
            Assert.Equal(
                expected: message.TickId,
                actual: settlement.TickId);
            Assert.Equal(
                expected: message.CorrelationId,
                actual: settlement.CorrelationId);

            Assert.Equal(
                expected: 3,
                actual: ledgerRepository.Entries.Count);
            Assert.All(
                collection: ledgerRepository.Entries,
                action: entry => Assert.Equal(
                    expected: ledgerTime,
                    actual: entry.OccurredAtUtc));
            Assert.Equal(
                expected: CityBudgetLedgerEntryKind.Revenue,
                actual: ledgerRepository.Entries[0].Kind);
            Assert.Equal(
                expected: CityBudgetCategory.Taxation,
                actual: ledgerRepository.Entries[0].Category);
            Assert.Equal(
                expected: 10m,
                actual: ledgerRepository.Entries[0].Amount.Amount);
            Assert.Equal(
                expected: CityBudgetLedgerEntryKind.Revenue,
                actual: ledgerRepository.Entries[1].Kind);
            Assert.Equal(
                expected: CityBudgetCategory.Commerce,
                actual: ledgerRepository.Entries[1].Category);
            Assert.Equal(
                expected: 5m,
                actual: ledgerRepository.Entries[1].Amount.Amount);
            Assert.Equal(
                expected: CityBudgetLedgerEntryKind.Expense,
                actual: ledgerRepository.Entries[2].Kind);
            Assert.Equal(
                expected: CityBudgetCategory.Operations,
                actual: ledgerRepository.Entries[2].Category);
            Assert.Equal(
                expected: 32.05m,
                actual: ledgerRepository.Entries[2].Amount.Amount);

            Assert.Equal(
                expected: -17.05m,
                actual: budget.Balance.Amount);
            Assert.Equal(
                expected: 15m,
                actual: budget.TotalTaxIncome.Amount);
            Assert.Equal(
                expected: 10m,
                actual: budget.TotalIncomeTaxIncome.Amount);
            Assert.Equal(
                expected: 5m,
                actual: budget.TotalSalesTaxIncome.Amount);
            Assert.Equal(
                expected: 32.05m,
                actual: budget.TotalCityExpenses.Amount);
            Assert.Equal(
                expected: 50m,
                actual: budget.TotalRetailTurnover.Amount);
            Assert.Equal(
                expected: 100m,
                actual: budget.TotalGrossPayroll.Amount);
            Assert.Equal(
                expected: 90m,
                actual: budget.TotalNetPayroll.Amount);

            TestLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(
                expected: LogLevel.Information,
                actual: entry.LogLevel);
            Assert.Contains(
                expectedSubstring: "Applied city economy settlement",
                actualString: entry.Message);
        }

        private static CityEconomyDailySettlementV1 CreateMessage()
        {
            return new CityEconomyDailySettlementV1(
                CityId: Guid.Parse("6c848f9a-28ca-4753-a22b-395dc347e691"),
                TickId: 17,
                CurrentDate: new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 6),
                SettledDays: 1,
                HouseholdCount: 10,
                ResidentCount: 20,
                GrossPayrollAmount: 100m,
                IncomeTaxAmount: 10m,
                NetPayrollAmount: 90m,
                RetailTurnoverAmount: 50m,
                RetailTaxAmount: 5m,
                HousingSpendAmount: 20m,
                CorrelationId: "settlement-17",
                OccurredAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 12,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }

        private sealed class TestCityBudgetRepository : ICityBudgetRepository
        {
            private readonly Dictionary<Guid, CityBudget> budgets = [];

            public List<CityBudget> AddedBudgets { get; } = [];

            public Task<CityBudget?> GetByCityAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                budgets.TryGetValue(
                    key: cityId,
                    value: out CityBudget? budget);
                return Task.FromResult(budget);
            }

            public Task<IReadOnlyList<CityBudget>> ListAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<CityBudget>>(budgets.Values.ToList());
            }

            public void Add(CityBudget cityBudget)
            {
                budgets[cityBudget.CityId] = cityBudget;
                AddedBudgets.Add(cityBudget);
            }
        }

        private sealed class TestCityBudgetLedgerRepository : ICityBudgetLedgerRepository
        {
            public List<CityBudgetLedgerEntry> Entries { get; } = [];

            public Task AddAsync(
                CityBudgetLedgerEntry entry,
                CancellationToken cancellationToken = default)
            {
                Entries.Add(entry);
                return Task.CompletedTask;
            }

            public Task<bool> ExistsAsync(
                Guid cityId,
                CityBudgetLedgerEntryKind kind,
                string referenceCode,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CursorPagedResult<CityBudgetLedgerEntry>> GetSliceByCityAsync(
                Guid cityId,
                LedgerCursor? cursor,
                int pageSize,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityBudgetOperationalExpenseSnapshot> GetOperationalExpenseSnapshotAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class TestCityBudgetSettlementRepository : ICityBudgetSettlementRepository
        {
            public bool ExistsResult { get; init; }

            public List<CityBudgetSettlement> AddedSettlements { get; } = [];

            public Task<bool> ExistsAsync(
                Guid cityId,
                long tickId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(ExistsResult);
            }

            public Task AddAsync(
                CityBudgetSettlement settlement,
                CancellationToken cancellationToken = default)
            {
                AddedSettlements.Add(settlement);
                return Task.CompletedTask;
            }
        }

        private sealed class TestEconomyUnitOfWork : IEconomyUnitOfWork
        {
            public int SaveChangesCalls { get; private set; }

            public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                SaveChangesCalls++;
                return Task.CompletedTask;
            }

            public Task ExecuteInTransactionAsync(
                Func<CancellationToken, Task> action,
                CancellationToken cancellationToken,
                IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
            {
                throw new NotSupportedException();
            }

            public Task<TResult> ExecuteInTransactionAsync<TResult>(
                Func<CancellationToken, Task<TResult>> operation,
                CancellationToken cancellationToken,
                IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
            {
                throw new NotSupportedException();
            }
        }
    }
}
