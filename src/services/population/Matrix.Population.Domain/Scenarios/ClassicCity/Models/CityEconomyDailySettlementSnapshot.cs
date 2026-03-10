using Matrix.BuildingBlocks.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityEconomyDailySettlementSnapshot(
        DateOnly CurrentDate,
        int SettledDays,
        int HouseholdCount,
        int ResidentCount,
        Money GrossPayroll,
        Money IncomeTax,
        Money NetPayroll,
        Money RetailTurnover,
        Money RetailTax,
        Money HousingSpend);
}
