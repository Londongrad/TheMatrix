namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record CityEmploymentCatalogDto(
        IReadOnlyList<string> JobTitles);
}
