using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.GetCityOperationalBudgetPressure
{
    public sealed class CityOperationalBudgetPressureProjectionService(
        ICityBudgetRepository budgetRepository,
        ICityBudgetLedgerRepository budgetLedgerRepository,
        ICityBudgetAllocationRepository allocationRepository,
        ICityEconomyProgressionStateRepository progressionStateRepository)
        : ICityOperationalBudgetPressureProjectionService
    {
        public async Task<CityOperationalBudgetPressureDto> GetAsync(
            Guid cityId,
            CancellationToken cancellationToken = default)
        {
            CityBudget budget = await budgetRepository.GetByCityAsync(
                                    cityId: cityId,
                                    cancellationToken: cancellationToken) ??
                                new CityBudget(
                                    id: CityBudgetId.New(),
                                    cityId: cityId);
            CityBudgetOperationalExpenseSnapshot snapshot =
                await budgetLedgerRepository.GetOperationalExpenseSnapshotAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            IReadOnlyList<CityBudgetAllocation> allocations = await allocationRepository.ListByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            CityEconomyProgressionState? progressionState = await progressionStateRepository.GetByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            var availableAmounts = allocations.ToDictionary(
                keySelector: x => x.Category,
                elementSelector: x => CityOperationalBudgetControlPolicy.NormalizeAvailableAmount(
                    x.GetAvailableAmount()
                       .Amount));
            decimal pressureIndex = CalculatePressureIndex(
                balance: budget.Balance.Amount,
                totalCityExpenses: budget.TotalCityExpenses.Amount,
                municipalOperationsExpenses: snapshot.TotalMunicipalOperationsExpenses);
            decimal generalAvailableAmount = ResolveAvailableAmount(
                category: CityBudgetCategory.General,
                availableAmounts: availableAmounts,
                balance: budget.Balance.Amount);
            decimal operationsAvailableAmount = ResolveAvailableAmount(
                category: CityBudgetCategory.Operations,
                availableAmounts: availableAmounts,
                balance: budget.Balance.Amount);
            decimal infrastructureAvailableAmount = ResolveAvailableAmount(
                category: CityBudgetCategory.Infrastructure,
                availableAmounts: availableAmounts,
                balance: budget.Balance.Amount);
            decimal healthcareAvailableAmount = ResolveAvailableAmount(
                category: CityBudgetCategory.Healthcare,
                availableAmounts: availableAmounts,
                balance: budget.Balance.Amount);

            return new CityOperationalBudgetPressureDto(
                CityId: cityId,
                EffectiveTickId: progressionState?.LastCompletedTickId ?? 0,
                EffectiveAtUtc: progressionState?.UpdatedAtUtc,
                UnitKind: budget.UnitKind.ToString(),
                UnitCode: budget.UnitCode,
                UnitDisplayName: budget.UnitDisplayName,
                UnitSymbol: budget.UnitSymbol,
                Balance: budget.Balance.Amount,
                TotalCityExpenses: budget.TotalCityExpenses.Amount,
                MunicipalOperationsExpenses: snapshot.TotalMunicipalOperationsExpenses,
                InfrastructureOperationsExpenses: snapshot.InfrastructureOperationsExpenses,
                EmergencyOperationsExpenses: snapshot.EmergencyOperationsExpenses,
                GeneralAvailableAmount: generalAvailableAmount,
                OperationsAvailableAmount: operationsAvailableAmount,
                InfrastructureAvailableAmount: infrastructureAvailableAmount,
                HealthcareAvailableAmount: healthcareAvailableAmount,
                GeneralAuthorizationLevel: CityOperationalBudgetControlPolicy.ResolveAuthorizationLevel(
                    availableAmount: generalAvailableAmount,
                    pressureIndex: pressureIndex),
                OperationsAuthorizationLevel: CityOperationalBudgetControlPolicy.ResolveAuthorizationLevel(
                    availableAmount: operationsAvailableAmount,
                    pressureIndex: pressureIndex),
                InfrastructureAuthorizationLevel: CityOperationalBudgetControlPolicy.ResolveAuthorizationLevel(
                    availableAmount: infrastructureAvailableAmount,
                    pressureIndex: pressureIndex),
                HealthcareAuthorizationLevel: CityOperationalBudgetControlPolicy.ResolveAuthorizationLevel(
                    availableAmount: healthcareAvailableAmount,
                    pressureIndex: pressureIndex),
                LastMunicipalExpenseAtUtc: snapshot.LastMunicipalExpenseAtUtc?.ToString("O"),
                PressureIndex: pressureIndex);
        }

        private static decimal CalculatePressureIndex(
            decimal balance,
            decimal totalCityExpenses,
            decimal municipalOperationsExpenses)
        {
            decimal operatingBurden = totalCityExpenses <= 0m
                ? 0m
                : ClampUnit(municipalOperationsExpenses / totalCityExpenses);
            decimal liquidityPressure = municipalOperationsExpenses <= 0m
                ? balance < 0m
                    ? 0.60m
                    : 0m
                : balance <= 0m
                    ? 1m
                    : ClampUnit(1m - (balance / (municipalOperationsExpenses * 2m)));
            decimal composite = (liquidityPressure * 0.60m) + (operatingBurden * 0.40m);

            return decimal.Round(
                d: ClampUnit(composite),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static decimal ClampUnit(decimal value)
        {
            return Math.Min(
                val1: 1m,
                val2: Math.Max(
                    val1: 0m,
                    val2: value));
        }

        private static decimal ResolveAvailableAmount(
            CityBudgetCategory category,
            IReadOnlyDictionary<CityBudgetCategory, decimal> availableAmounts,
            decimal balance)
        {
            if (availableAmounts.TryGetValue(
                    key: category,
                    value: out decimal availableAmount))
                return availableAmount;

            return CityOperationalBudgetControlPolicy.NormalizeAvailableAmount(balance);
        }
    }
}
