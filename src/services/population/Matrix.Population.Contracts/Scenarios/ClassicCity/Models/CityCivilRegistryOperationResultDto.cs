namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record class CityCivilRegistryOperationResultDto(
        string Action,
        DateTimeOffset RecordedAtUtc,
        CityResidentDetailsDto FirstResident,
        CityResidentDetailsDto SecondResident);
}
