using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy;
using Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Application.UseCases.Ledger.Common;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.ValueObjects;
using System.Data;

namespace Matrix.Economy.Application.Tests.TestSupport;

internal static class EconomyApplicationTestSupport
{
    internal static CityBudget CreateBudget(
        Guid cityId,
        CityBudgetUnitProfile? unitProfile = null)
    {
        return new CityBudget(
            id: CityBudgetId.New(),
            cityId: cityId,
            unitProfile: unitProfile ?? CityBudgetUnitProfile.DefaultMoney());
    }

    internal static CityBudgetLedgerEntry CreateBudgetEntry(
        Guid cityId,
        CityBudgetLedgerEntryKind kind,
        decimal amount,
        string title = "Entry")
    {
        return new CityBudgetLedgerEntry(
            id: Guid.NewGuid(),
            cityId: cityId,
            occurredAtUtc: new DateTimeOffset(2048, 5, 6, 10, 0, 0, TimeSpan.Zero),
            kind: kind,
            category: CityBudgetCategory.General,
            amount: Money.FromDecimal(amount),
            title: title,
            description: null,
            source: CityBudgetLedgerEntrySource.Manual,
            referenceCode: null);
    }

    internal static CityBudgetAllocation CreateAllocation(
        Guid cityId,
        CityBudgetCategory category,
        decimal targetAmount,
        decimal spentAmount = 0m)
    {
        var allocation = new CityBudgetAllocation(
            id: Guid.NewGuid(),
            cityId: cityId,
            category: category,
            createdAtUtc: new DateTimeOffset(2048, 5, 6, 9, 0, 0, TimeSpan.Zero),
            unitProfile: CityBudgetUnitProfile.DefaultMoney(),
            targetAmount: Money.FromDecimal(targetAmount));

        if (spentAmount > 0m)
            allocation.RecordExpense(
                amount: Money.FromDecimal(spentAmount),
                updatedAtUtc: new DateTimeOffset(2048, 5, 6, 10, 0, 0, TimeSpan.Zero));

        return allocation;
    }

    internal static CityBusiness CreateBusiness(
        Guid cityId,
        string name,
        CityBusinessKind kind,
        decimal initialCapital = 0m)
    {
        return new CityBusiness(
            id: Guid.NewGuid(),
            cityId: cityId,
            name: name,
            externalReferenceCode: $"{name}-ext",
            templateKey: $"{name}-tpl",
            kind: kind,
            createdAtUtc: new DateTimeOffset(2048, 5, 6, 9, 0, 0, TimeSpan.Zero),
            unitProfile: CityBudgetUnitProfile.DefaultMoney(),
            initialCapital: Money.FromDecimal(initialCapital));
    }

    internal static CityHouseholdAccount CreateHouseholdAccount(
        Guid cityId,
        string name,
        decimal openingBalance = 0m)
    {
        return new CityHouseholdAccount(
            id: Guid.NewGuid(),
            cityId: cityId,
            name: name,
            externalReferenceCode: $"{name}-ext",
            createdAtUtc: new DateTimeOffset(2048, 5, 6, 9, 0, 0, TimeSpan.Zero),
            unitProfile: CityBudgetUnitProfile.DefaultMoney(),
            openingBalance: Money.FromDecimal(openingBalance));
    }

    internal static CityHouseholdObligation CreateHouseholdObligation(
        Guid cityId,
        Guid householdAccountId,
        Guid providerBusinessId,
        string name,
        CityHouseholdObligationKind kind,
        CityHouseholdObligationBillingCadence cadence,
        decimal chargeAmount,
        decimal taxAmount)
    {
        return new CityHouseholdObligation(
            id: Guid.NewGuid(),
            cityId: cityId,
            householdAccountId: householdAccountId,
            providerBusinessId: providerBusinessId,
            name: name,
            kind: kind,
            billingCadence: cadence,
            createdAtUtc: new DateTimeOffset(2048, 5, 6, 9, 0, 0, TimeSpan.Zero),
            firstChargeDueAtUtc: new DateTimeOffset(2048, 5, 7, 9, 0, 0, TimeSpan.Zero),
            unitProfile: CityBudgetUnitProfile.DefaultMoney(),
            chargeAmount: Money.FromDecimal(chargeAmount),
            taxAmount: Money.FromDecimal(taxAmount));
    }

    internal sealed class FakeCityEconomyBootstrapService : ICityEconomyBootstrapService
    {
        public (Guid CityId, string SimulationKind, string? EconomyProfile, DateTimeOffset CreatedAtUtc)? Request { get; private set; }
        public CityEconomyBootstrapResultDto Result { get; set; } = new(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            BudgetCreated: true,
            CreatedAllocations: 4,
            CreatedBusinesses: 8,
            UnitKind: "Currency",
            UnitCode: "MNY",
            UnitDisplayName: "Money",
            UnitSymbol: "В¤");

        public Task<CityEconomyBootstrapResultDto> BootstrapAsync(
            Guid cityId,
            string simulationKind,
            string? economyProfile,
            DateTimeOffset createdAtUtc,
            CancellationToken cancellationToken = default)
        {
            Request = (cityId, simulationKind, economyProfile, createdAtUtc);
            return Task.FromResult(Result);
        }
    }

    internal sealed class FakeCityBudgetRepository : ICityBudgetRepository
    {
        public CityBudget? BudgetByCity { get; set; }
        public IReadOnlyList<CityBudget> Budgets { get; set; } = Array.Empty<CityBudget>();
        public Guid? RequestedCityId { get; private set; }
        public List<CityBudget> AddedBudgets { get; } = [];

        public Task<CityBudget?> GetByCityAsync(Guid cityId, CancellationToken cancellationToken = default)
        {
            RequestedCityId = cityId;
            return Task.FromResult(BudgetByCity);
        }

        public Task<IReadOnlyList<CityBudget>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Budgets);
        }

        public void Add(CityBudget cityBudget)
        {
            AddedBudgets.Add(cityBudget);
        }
    }

    internal sealed class FakeCityBudgetAllocationRepository : ICityBudgetAllocationRepository
    {
        public IReadOnlyList<CityBudgetAllocation> Allocations { get; set; } = Array.Empty<CityBudgetAllocation>();
        public Guid? RequestedCityId { get; private set; }
        public List<CityBudgetAllocation> AddedAllocations { get; } = [];

        public Task<CityBudgetAllocation?> GetByCityAndCategoryAsync(
            Guid cityId,
            CityBudgetCategory category,
            CancellationToken cancellationToken = default)
        {
            RequestedCityId = cityId;
            return Task.FromResult(Allocations.FirstOrDefault(x => x.CityId == cityId && x.Category == category));
        }

        public Task<IReadOnlyList<CityBudgetAllocation>> ListByCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            RequestedCityId = cityId;
            return Task.FromResult<IReadOnlyList<CityBudgetAllocation>>(Allocations.Where(x => x.CityId == cityId).ToArray());
        }

        public void Add(CityBudgetAllocation allocation)
        {
            AddedAllocations.Add(allocation);
        }
    }

    internal sealed class FakeCityBusinessRepository : ICityBusinessRepository
    {
        public IReadOnlyList<CityBusiness> Businesses { get; set; } = Array.Empty<CityBusiness>();
        public Guid? RequestedCityId { get; private set; }
        public List<CityBusiness> AddedBusinesses { get; } = [];

        public Task<CityBusiness?> GetByIdAsync(Guid businessId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Businesses.FirstOrDefault(x => x.Id == businessId));
        }

        public Task<CityBusiness?> GetByCityAndExternalReferenceCodeAsync(
            Guid cityId,
            string externalReferenceCode,
            CancellationToken cancellationToken = default)
        {
            RequestedCityId = cityId;
            return Task.FromResult(Businesses.FirstOrDefault(x => x.CityId == cityId && x.ExternalReferenceCode == externalReferenceCode));
        }

        public Task<CityBusiness?> GetByCityAndTemplateKeyAsync(
            Guid cityId,
            string templateKey,
            CancellationToken cancellationToken = default)
        {
            RequestedCityId = cityId;
            return Task.FromResult(Businesses.FirstOrDefault(x => x.CityId == cityId && x.TemplateKey == templateKey));
        }

        public Task<IReadOnlyList<CityBusiness>> ListByCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            RequestedCityId = cityId;
            return Task.FromResult<IReadOnlyList<CityBusiness>>(Businesses.Where(x => x.CityId == cityId).ToArray());
        }

        public void Add(CityBusiness cityBusiness)
        {
            AddedBusinesses.Add(cityBusiness);
        }
    }

    internal sealed class FakeCityHouseholdAccountRepository : ICityHouseholdAccountRepository
    {
        public IReadOnlyList<CityHouseholdAccount> Accounts { get; set; } = Array.Empty<CityHouseholdAccount>();
        public Guid? RequestedCityId { get; private set; }
        public List<CityHouseholdAccount> AddedAccounts { get; } = [];

        public Task<CityHouseholdAccount?> GetByIdAsync(Guid householdAccountId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Accounts.FirstOrDefault(x => x.Id == householdAccountId));
        }

        public Task<CityHouseholdAccount?> GetByCityAndExternalReferenceCodeAsync(
            Guid cityId,
            string externalReferenceCode,
            CancellationToken cancellationToken = default)
        {
            RequestedCityId = cityId;
            return Task.FromResult(Accounts.FirstOrDefault(x => x.CityId == cityId && x.ExternalReferenceCode == externalReferenceCode));
        }

        public Task<IReadOnlyList<CityHouseholdAccount>> ListByCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            RequestedCityId = cityId;
            return Task.FromResult<IReadOnlyList<CityHouseholdAccount>>(Accounts.Where(x => x.CityId == cityId).ToArray());
        }

        public void Add(CityHouseholdAccount householdAccount)
        {
            AddedAccounts.Add(householdAccount);
        }
    }

    internal sealed class FakeCityHouseholdObligationRepository : ICityHouseholdObligationRepository
    {
        public IReadOnlyList<CityHouseholdObligation> Obligations { get; set; } = Array.Empty<CityHouseholdObligation>();
        public Guid? RequestedCityId { get; private set; }
        public List<CityHouseholdObligation> AddedObligations { get; } = [];

        public Task<CityHouseholdObligation?> GetByIdAsync(Guid obligationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Obligations.FirstOrDefault(x => x.Id == obligationId));
        }

        public Task<IReadOnlyList<CityHouseholdObligation>> ListByCityAsync(Guid cityId, CancellationToken cancellationToken = default)
        {
            RequestedCityId = cityId;
            return Task.FromResult<IReadOnlyList<CityHouseholdObligation>>(Obligations.Where(x => x.CityId == cityId).ToArray());
        }

        public Task<IReadOnlyList<CityHouseholdObligation>> ListDueByCityAsync(Guid cityId, DateTimeOffset asOfUtc, CancellationToken cancellationToken = default)
        {
            RequestedCityId = cityId;
            return Task.FromResult<IReadOnlyList<CityHouseholdObligation>>(Obligations.Where(x => x.CityId == cityId && x.IsDue(asOfUtc)).ToArray());
        }

        public Task<IReadOnlyList<CityHouseholdObligation>> ListByHouseholdAsync(Guid householdAccountId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<CityHouseholdObligation>>(Obligations.Where(x => x.HouseholdAccountId == householdAccountId).ToArray());
        }

        public Task<IReadOnlyList<CityHouseholdObligation>> ListByHouseholdsAsync(IReadOnlyCollection<Guid> householdAccountIds, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<CityHouseholdObligation>>(Obligations.Where(x => householdAccountIds.Contains(x.HouseholdAccountId)).ToArray());
        }

        public void Add(CityHouseholdObligation obligation)
        {
            AddedObligations.Add(obligation);
        }
    }

    internal sealed class FakeCityOperationalBudgetPressureProjectionService
        : ICityOperationalBudgetPressureProjectionService
    {
        public Guid? RequestedCityId { get; private set; }
        public CityOperationalBudgetPressureDto Result { get; set; } = new(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            EffectiveTickId: 42,
            EffectiveAtUtc: new DateTimeOffset(2048, 5, 6, 11, 0, 0, TimeSpan.Zero),
            UnitKind: "Currency",
            UnitCode: "MNY",
            UnitDisplayName: "Money",
            UnitSymbol: "¤",
            Balance: 1800m,
            TotalCityExpenses: 420m,
            MunicipalOperationsExpenses: 120m,
            InfrastructureOperationsExpenses: 80m,
            EmergencyOperationsExpenses: 30m,
            GeneralAvailableAmount: 900m,
            OperationsAvailableAmount: 500m,
            InfrastructureAvailableAmount: 300m,
            HealthcareAvailableAmount: 200m,
            GeneralAuthorizationLevel: "Open",
            OperationsAuthorizationLevel: "Watch",
            InfrastructureAuthorizationLevel: "Stable",
            HealthcareAuthorizationLevel: "Protected",
            LastMunicipalExpenseAtUtc: "2048-05-06T10:30:00Z",
            PressureIndex: 0.25m);

        public Task<CityOperationalBudgetPressureDto> GetAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            RequestedCityId = cityId;
            return Task.FromResult(Result);
        }
    }

    internal sealed class FakeCityEconomyCostProfileStateRepository
        : ICityEconomyCostProfileStateRepository
    {
        public CityEconomyCostProfileState? StateByCity { get; set; }
        public Guid? RequestedCityId { get; private set; }
        public List<CityEconomyCostProfileState> AddedStates { get; } = [];

        public Task<CityEconomyCostProfileState?> GetByCityAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            RequestedCityId = cityId;
            return Task.FromResult(StateByCity);
        }

        public Task AddAsync(
            CityEconomyCostProfileState state,
            CancellationToken cancellationToken = default)
        {
            AddedStates.Add(state);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeCityOperationalBudgetSignalPublisher
        : ICityOperationalBudgetSignalPublisher
    {
        public List<PublishedSignal> PublishedSignals { get; } = [];

        public Task PublishClassicCityOperationalBudgetPressureSnapshotAsync(
            CityOperationalBudgetPressureDto snapshot,
            DateTimeOffset effectiveAtUtc,
            DateTimeOffset occurredAtUtc,
            CancellationToken cancellationToken = default)
        {
            PublishedSignals.Add(new PublishedSignal(
                Snapshot: snapshot,
                EffectiveAtUtc: effectiveAtUtc,
                OccurredAtUtc: occurredAtUtc));
            return Task.CompletedTask;
        }

        internal sealed record PublishedSignal(
            CityOperationalBudgetPressureDto Snapshot,
            DateTimeOffset EffectiveAtUtc,
            DateTimeOffset OccurredAtUtc);
    }

    internal sealed class FakeCityPopulationSignalPublisher
        : ICityPopulationSignalPublisher
    {
        public List<ClassicCityCostOfLivingSnapshotV1> CostOfLivingSnapshots { get; } = [];
        public List<ClassicCityServiceQualitySnapshotV1> ServiceQualitySnapshots { get; } = [];
        public List<ClassicCityEmployerFinancialStressBatchV1> EmployerFinancialStressBatches { get; } = [];
        public List<ClassicCityHouseholdFinancialStressBatchV1> HouseholdFinancialStressBatches { get; } = [];

        public Task PublishClassicCityCostOfLivingSnapshotAsync(
            ClassicCityCostOfLivingSnapshotV1 snapshot,
            CancellationToken cancellationToken = default)
        {
            CostOfLivingSnapshots.Add(snapshot);
            return Task.CompletedTask;
        }

        public Task PublishClassicCityServiceQualitySnapshotAsync(
            ClassicCityServiceQualitySnapshotV1 snapshot,
            CancellationToken cancellationToken = default)
        {
            ServiceQualitySnapshots.Add(snapshot);
            return Task.CompletedTask;
        }

        public Task PublishClassicCityEmployerFinancialStressBatchAsync(
            ClassicCityEmployerFinancialStressBatchV1 batch,
            CancellationToken cancellationToken = default)
        {
            EmployerFinancialStressBatches.Add(batch);
            return Task.CompletedTask;
        }

        public Task PublishClassicCityHouseholdFinancialStressBatchAsync(
            ClassicCityHouseholdFinancialStressBatchV1 batch,
            CancellationToken cancellationToken = default)
        {
            HouseholdFinancialStressBatches.Add(batch);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeCityBusinessLedgerRepository : ICityBusinessLedgerRepository
    {
        public List<CityBusinessLedgerEntry> AddedEntries { get; } = [];

        public Task AddAsync(
            CityBusinessLedgerEntry entry,
            CancellationToken cancellationToken = default)
        {
            AddedEntries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(
            Guid businessId,
            CityBusinessLedgerEntryKind kind,
            string referenceCode,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<CursorPagedResult<CityBusinessLedgerEntry>> GetSliceByBusinessAsync(
            Guid businessId,
            LedgerCursor? cursor,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CursorPagedResult<CityBusinessLedgerEntry>([], pageSize, null));
        }
    }

    internal sealed class FakeCityHouseholdAccountLedgerRepository : ICityHouseholdAccountLedgerRepository
    {
        public List<CityHouseholdAccountLedgerEntry> AddedEntries { get; } = [];

        public Task AddAsync(
            CityHouseholdAccountLedgerEntry entry,
            CancellationToken cancellationToken = default)
        {
            AddedEntries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(
            Guid householdAccountId,
            CityHouseholdAccountLedgerEntryKind kind,
            string referenceCode,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<CursorPagedResult<CityHouseholdAccountLedgerEntry>> GetSliceByHouseholdAccountAsync(
            Guid householdAccountId,
            LedgerCursor? cursor,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CursorPagedResult<CityHouseholdAccountLedgerEntry>([], pageSize, null));
        }
    }

    internal sealed class FakeCityBudgetLedgerRepository : ICityBudgetLedgerRepository
    {
        public List<CityBudgetLedgerEntry> AddedEntries { get; } = [];
        public CityBudgetOperationalExpenseSnapshot Snapshot { get; set; } = new(
            TotalMunicipalOperationsExpenses: 0m,
            InfrastructureOperationsExpenses: 0m,
            EmergencyOperationsExpenses: 0m,
            LastMunicipalExpenseAtUtc: null);

        public Task AddAsync(
            CityBudgetLedgerEntry entry,
            CancellationToken cancellationToken = default)
        {
            AddedEntries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(
            Guid cityId,
            CityBudgetLedgerEntryKind kind,
            string referenceCode,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<CursorPagedResult<CityBudgetLedgerEntry>> GetSliceByCityAsync(
            Guid cityId,
            LedgerCursor? cursor,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CursorPagedResult<CityBudgetLedgerEntry>([], pageSize, null));
        }

        public Task<CityBudgetOperationalExpenseSnapshot> GetOperationalExpenseSnapshotAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Snapshot);
        }
    }

    internal sealed class FakeEconomyUnitOfWork : IEconomyUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }
        public int TransactionCallCount { get; private set; }
        public IsolationLevel? LastIsolationLevel { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            TransactionCallCount++;
            LastIsolationLevel = isolationLevel;
            await action(cancellationToken);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            TransactionCallCount++;
            LastIsolationLevel = isolationLevel;
            return await action(cancellationToken);
        }
    }

    internal sealed class FrozenTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        public override DateTimeOffset GetUtcNow()
        {
            return UtcNow;
        }
    }
}

