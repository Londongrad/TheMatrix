namespace Matrix.CityCore.Application.UseCases.Cities.GetSuggestedCityNames
{
    public sealed record SuggestedCityNamesDto(
        string? Seed,
        IReadOnlyList<string> Names);
}