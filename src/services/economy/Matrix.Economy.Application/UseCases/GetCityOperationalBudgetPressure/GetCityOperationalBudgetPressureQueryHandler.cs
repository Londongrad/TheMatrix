using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.ValueObjects;
using MediatR;

namespace Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure
{
    public sealed class GetCityOperationalBudgetPressureQueryHandler(
        ICityBudgetRepository budgetRepository,
        ICityBudgetLedgerRepository budgetLedgerRepository)
        : IRequestHandler<GetCityOperationalBudgetPressureQuery, CityOperationalBudgetPressureDto>
    {
        public async Task<CityOperationalBudgetPressureDto> Handle(
            GetCityOperationalBudgetPressureQuery request,
            CancellationToken cancellationToken)
        {
            CityBudget budget = await budgetRepository.GetByCityAsync(
                                    cityId: request.CityId,
                                    cancellationToken: cancellationToken) ??
                                new CityBudget(
                                    id: CityBudgetId.New(),
                                    cityId: request.CityId);
            CityBudgetOperationalExpenseSnapshot snapshot =
                await budgetLedgerRepository.GetOperationalExpenseSnapshotAsync(
                    cityId: request.CityId,
                    cancellationToken: cancellationToken);

            return new CityOperationalBudgetPressureDto(
                CityId: request.CityId,
                UnitKind: budget.UnitKind.ToString(),
                UnitCode: budget.UnitCode,
                UnitDisplayName: budget.UnitDisplayName,
                UnitSymbol: budget.UnitSymbol,
                Balance: budget.Balance.Amount,
                TotalCityExpenses: budget.TotalCityExpenses.Amount,
                MunicipalOperationsExpenses: snapshot.TotalMunicipalOperationsExpenses,
                InfrastructureOperationsExpenses: snapshot.InfrastructureOperationsExpenses,
                EmergencyOperationsExpenses: snapshot.EmergencyOperationsExpenses,
                LastMunicipalExpenseAtUtc: snapshot.LastMunicipalExpenseAtUtc?.ToString("O"),
                PressureIndex: CalculatePressureIndex(
                    balance: budget.Balance.Amount,
                    totalCityExpenses: budget.TotalCityExpenses.Amount,
                    municipalOperationsExpenses: snapshot.TotalMunicipalOperationsExpenses));
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
                ? (balance < 0m ? 0.60m : 0m)
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
    }
}
