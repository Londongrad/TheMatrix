namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record CityEducationInstitutionDto(
        Guid InstitutionId,
        string EducationLevel,
        int ResidentCount);

    public sealed record CityEducationCatalogDto(IReadOnlyList<CityEducationInstitutionDto> CurrentInstitutions);
}
