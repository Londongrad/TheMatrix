namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.RunCityMunicipalOperatingCycle
{
    public sealed record RunCityMunicipalOperatingCycleResultDto(
        Guid CityId,
        int AllocationCategoriesTouched,
        int ProviderPayments,
        decimal TotalDisbursedAmount);
}
