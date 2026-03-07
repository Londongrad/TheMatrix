namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record CityEducationOperationResultDto(
        string Action,
        DateTimeOffset RecordedAtUtc,
        CityResidentDetailsDto Resident);
}
