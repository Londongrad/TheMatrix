using Matrix.BuildingBlocks.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityResidentIncomeSettlementProfile(
        Money GrossIncome,
        Money TaxWithheld,
        Money NetIncome);
}
