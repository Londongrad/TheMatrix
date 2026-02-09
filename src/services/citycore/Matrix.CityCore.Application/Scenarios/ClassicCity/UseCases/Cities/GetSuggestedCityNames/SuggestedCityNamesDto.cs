namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetSuggestedCityNames
{
    public sealed record SuggestedCityNamesDto(
        string? Seed,
        IReadOnlyList<string> Names);
}
