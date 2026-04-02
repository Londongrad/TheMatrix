namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record CityEmploymentWorkplaceDto(
        Guid WorkplaceId,
        Guid? WorkplaceAnchorId,
        string JobTitle,
        int ResidentCount);

    public sealed record CityEmploymentCatalogDto(
        IReadOnlyList<string> JobTitles,
        IReadOnlyList<CityEmploymentWorkplaceDto> CurrentWorkplaces);
}
