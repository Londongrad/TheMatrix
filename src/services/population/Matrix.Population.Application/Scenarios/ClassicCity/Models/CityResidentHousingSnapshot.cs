using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Models
{
    public sealed record class CityResidentHousingSnapshot(
        HouseholdId HouseholdId,
        HousingStatus HousingStatus,
        DistrictId? DistrictId,
        ResidentialBuildingId? ResidentialBuildingId);
}
