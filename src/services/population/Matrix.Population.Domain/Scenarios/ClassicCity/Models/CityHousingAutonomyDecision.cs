using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public enum CityHousingAutonomyDecisionType
    {
        FindHousing = 1,
        LoseHousing = 2
    }

    public sealed record CityHousingAutonomyDecision(
        CityHousingAutonomyDecisionType Type,
        HouseholdId HouseholdId);
}
