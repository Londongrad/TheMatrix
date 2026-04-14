namespace Matrix.Population.Contracts.Scenarios.ClassicCity.Models
{
    public sealed record class CityPopulationDistrictPressureDto(
        Guid CityId,
        string GeneratedAtUtc,
        IReadOnlyList<CityPopulationDistrictPressureItemDto> Districts);

    public sealed record class CityPopulationDistrictPressureItemDto(
        Guid DistrictId,
        int ResidentCount,
        int HouseholdCount,
        int HomelessResidentCount,
        decimal AverageHealth,
        decimal AverageStress,
        decimal AverageHappiness,
        int ActiveIllnessCount,
        int SevereIllnessCount,
        decimal UtilityContinuityIndex,
        decimal UtilityIncidentPressureIndex,
        decimal HousingFragilityIndex,
        decimal PopulationPressureIndex);
}
