namespace Matrix.CityCore.Application.UseCases.Cities.GetGenerationCatalog
{
    public sealed record CityGenerationCatalogDto(
        IReadOnlyList<string> CityNamePresets,
        IReadOnlyList<string> DistrictNamePresets,
        IReadOnlyList<string> StreetNamePresets);
}