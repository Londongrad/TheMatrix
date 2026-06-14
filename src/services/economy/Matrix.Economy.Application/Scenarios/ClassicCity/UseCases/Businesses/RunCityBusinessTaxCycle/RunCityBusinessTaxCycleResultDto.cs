namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RunCityBusinessTaxCycle
{
    public sealed record RunCityBusinessTaxCycleResultDto(
        Guid CityId,
        string BudgetCategory,
        int RemittedBusinesses,
        decimal TotalRemittedAmount);
}
