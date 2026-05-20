namespace Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Routing.Views
{
    public sealed record ResolveCityRoutesBatchView(
        IReadOnlyList<ResolvedCityRouteBatchItemView> Routes);

    public sealed record ResolvedCityRouteBatchItemView(
        int Index,
        bool Found,
        CityRouteView? Route);
}
