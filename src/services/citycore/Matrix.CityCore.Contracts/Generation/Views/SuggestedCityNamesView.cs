namespace Matrix.CityCore.Contracts.Generation.Views
{
    public sealed record SuggestedCityNamesView(
        string? Seed,
        string[] Names);
}
