namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard
{
    public sealed record CityPopulationDashboardReadModel(
        CityPopulationDashboardSnapshotReadModel Current,
        CityPopulationDashboardSnapshotReadModel? Yesterday,
        CityPopulationDashboardSnapshotReadModel? PreviousMonth,
        CityPopulationDashboardSnapshotReadModel? PreviousYear,
        IReadOnlyList<CityPopulationActivityEventReadModel> RecentEvents);
}
