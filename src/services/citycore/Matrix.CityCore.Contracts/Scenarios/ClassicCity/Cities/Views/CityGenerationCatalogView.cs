namespace Matrix.CityCore.Contracts.Scenarios.ClassicCity.Cities.Views
{
    public sealed record CityGenerationCatalogView(
        string[] CityNamePresets,
        string[] DistrictNamePresets,
        string[] StreetNamePresets);
}
