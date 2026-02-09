namespace Matrix.CityCore.Contracts.Scenarios.ClassicCity.Cities.Views
{
    public sealed record SimulationKindCatalogItemView(
        string Kind,
        string DisplayName,
        string Description,
        bool SupportsAutomaticPopulationBootstrap,
        bool IsDefault);
}
