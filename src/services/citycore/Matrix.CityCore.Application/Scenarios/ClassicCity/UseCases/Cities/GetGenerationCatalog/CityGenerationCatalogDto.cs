namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetGenerationCatalog
{
    public sealed record CityGenerationCatalogDto(
        IReadOnlyList<string> CityNamePresets,
        IReadOnlyList<string> DistrictNamePresets,
        IReadOnlyList<string> StreetNamePresets);
}
