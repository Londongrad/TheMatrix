namespace Matrix.Economy.Application.UseCases.BudgetOperations.RunCityMunicipalOperatingCycle
{
    public sealed record RunCityMunicipalOperatingCycleResultDto(
        Guid CityId,
        int AllocationCategoriesTouched,
        int ProviderPayments,
        decimal TotalDisbursedAmount);
}
