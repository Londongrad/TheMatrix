using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy;
using Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.ValueObjects;

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
            UnitSymbol: "В¤",
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
}
