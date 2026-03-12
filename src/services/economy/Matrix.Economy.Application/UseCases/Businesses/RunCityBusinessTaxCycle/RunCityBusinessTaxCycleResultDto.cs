namespace Matrix.Economy.Application.UseCases.Businesses.RunCityBusinessTaxCycle
{
    public sealed record RunCityBusinessTaxCycleResultDto(
        Guid CityId,
        string BudgetCategory,
        int RemittedBusinesses,
        decimal TotalRemittedAmount);
}
