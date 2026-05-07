using System.Data;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.BuildingBlocks.Infrastructure.Persistence;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Services;
using Matrix.Economy.Infrastructure.Consumers;
using Matrix.Economy.Infrastructure.Tests.TestSupport;
using Microsoft.Extensions.Logging;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Consumers;

public sealed class CityEconomyDailySettlementConsumerTests
{
    [Fact]
    public async Task ConsumeAsync_WhenSettlementAlreadyExists_LogsDebugAndSkipsMutation()
    {
        var budgetRepository = new TestCityBudgetRepository();
        var ledgerRepository = new TestCityBudgetLedgerRepository();
        var settlementRepository = new TestCityBudgetSettlementRepository { ExistsResult = true };
        var unitOfWork = new TestEconomyUnitOfWork();
        var logger = new TestLogger<CityEconomyDailySettlementConsumer>();
        var consumer = new CityEconomyDailySettlementConsumer(
            budgetRepository,
            ledgerRepository,
            settlementRepository,
            unitOfWork,
            new CityBudgetOperatingExpensePolicy(),
            new FrozenTimeProvider(new DateTimeOffset(2048, 5, 6, 12, 0, 0, TimeSpan.Zero)),
            logger);

        await consumer.ConsumeAsync(CreateMessage(), CancellationToken.None);

        Assert.Empty(budgetRepository.AddedBudgets);
        Assert.Empty(ledgerRepository.Entries);
        Assert.Empty(settlementRepository.AddedSettlements);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.LogLevel);
        Assert.Contains("Skipped duplicate city economy settlement", entry.Message);
    }

    [Fact]
    public async Task ConsumeAsync_WhenSettlementIsNew_AppliesBudgetAndWritesLedgersUsingInjectedTime()
    {
        DateTimeOffset ledgerTime = new(2048, 5, 6, 12, 34, 56, TimeSpan.Zero);
        var budgetRepository = new TestCityBudgetRepository();
        var ledgerRepository = new TestCityBudgetLedgerRepository();
        var settlementRepository = new TestCityBudgetSettlementRepository();
        var unitOfWork = new TestEconomyUnitOfWork();
        var logger = new TestLogger<CityEconomyDailySettlementConsumer>();
        var consumer = new CityEconomyDailySettlementConsumer(
            budgetRepository,
            ledgerRepository,
            settlementRepository,
            unitOfWork,
            new CityBudgetOperatingExpensePolicy(),
            new FrozenTimeProvider(ledgerTime),
            logger);
        CityEconomyDailySettlementV1 message = CreateMessage();

        await consumer.ConsumeAsync(message, CancellationToken.None);

        CityBudget budget = Assert.Single(budgetRepository.AddedBudgets);
        Assert.Equal(message.CityId, budget.CityId);
        Assert.Equal(2, unitOfWork.SaveChangesCalls);
        CityBudgetSettlement settlement = Assert.Single(settlementRepository.AddedSettlements);
        Assert.Equal(message.TickId, settlement.TickId);
        Assert.Equal(message.CorrelationId, settlement.CorrelationId);

        Assert.Equal(3, ledgerRepository.Entries.Count);
        Assert.All(ledgerRepository.Entries, entry => Assert.Equal(ledgerTime, entry.OccurredAtUtc));
        Assert.Equal(CityBudgetLedgerEntryKind.Revenue, ledgerRepository.Entries[0].Kind);
        Assert.Equal(CityBudgetCategory.Taxation, ledgerRepository.Entries[0].Category);
        Assert.Equal(10m, ledgerRepository.Entries[0].Amount.Amount);
        Assert.Equal(CityBudgetLedgerEntryKind.Revenue, ledgerRepository.Entries[1].Kind);
        Assert.Equal(CityBudgetCategory.Commerce, ledgerRepository.Entries[1].Category);
        Assert.Equal(5m, ledgerRepository.Entries[1].Amount.Amount);
        Assert.Equal(CityBudgetLedgerEntryKind.Expense, ledgerRepository.Entries[2].Kind);
        Assert.Equal(CityBudgetCategory.Operations, ledgerRepository.Entries[2].Category);
        Assert.Equal(32.05m, ledgerRepository.Entries[2].Amount.Amount);

        Assert.Equal(-17.05m, budget.Balance.Amount);
        Assert.Equal(15m, budget.TotalTaxIncome.Amount);
        Assert.Equal(10m, budget.TotalIncomeTaxIncome.Amount);
        Assert.Equal(5m, budget.TotalSalesTaxIncome.Amount);
        Assert.Equal(32.05m, budget.TotalCityExpenses.Amount);
        Assert.Equal(50m, budget.TotalRetailTurnover.Amount);
        Assert.Equal(100m, budget.TotalGrossPayroll.Amount);
        Assert.Equal(90m, budget.TotalNetPayroll.Amount);

        TestLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.LogLevel);
        Assert.Contains("Applied city economy settlement", entry.Message);
    }

    private static CityEconomyDailySettlementV1 CreateMessage()
    {
        return new CityEconomyDailySettlementV1(
            CityId: Guid.Parse("6c848f9a-28ca-4753-a22b-395dc347e691"),
            TickId: 17,
            CurrentDate: new DateOnly(2048, 5, 6),
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
            OccurredAtUtc: new DateTimeOffset(2048, 5, 6, 12, 0, 0, TimeSpan.Zero));
    }

    private sealed class TestCityBudgetRepository : ICityBudgetRepository
    {
        private readonly Dictionary<Guid, CityBudget> budgets = [];

        public List<CityBudget> AddedBudgets { get; } = [];

        public Task<CityBudget?> GetByCityAsync(Guid cityId, CancellationToken cancellationToken = default)
        {
            budgets.TryGetValue(cityId, out CityBudget? budget);
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

        public Task AddAsync(CityBudgetLedgerEntry entry, CancellationToken cancellationToken = default)
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
            Matrix.Economy.Application.UseCases.Ledger.Common.LedgerCursor? cursor,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure.CityBudgetOperationalExpenseSnapshot> GetOperationalExpenseSnapshotAsync(
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

        public Task<bool> ExistsAsync(Guid cityId, long tickId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ExistsResult);
        }

        public Task AddAsync(CityBudgetSettlement settlement, CancellationToken cancellationToken = default)
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
