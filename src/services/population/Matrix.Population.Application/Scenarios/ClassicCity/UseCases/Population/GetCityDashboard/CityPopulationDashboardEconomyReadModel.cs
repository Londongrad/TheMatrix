namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard
{
    public sealed record CityPopulationDashboardEconomyReadModel(
        int StableHouseholdCount,
        int StrainedHouseholdCount,
        int DeficitHouseholdCount,
        decimal? AverageCashReserveAmount,
        decimal? AverageDailyNetAmount);
}
