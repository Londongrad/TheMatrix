namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record CityEmploymentOperationResultDto(
        string Action,
        DateTimeOffset RecordedAtUtc,
        CityResidentDetailsDto Resident);
}
