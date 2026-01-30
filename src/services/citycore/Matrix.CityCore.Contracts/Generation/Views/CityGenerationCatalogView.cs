namespace Matrix.CityCore.Contracts.Generation.Views
{
    public sealed record CityGenerationCatalogView(
        string[] CityNamePresets,
        string[] DistrictNamePresets,
        string[] StreetNamePresets);
}
