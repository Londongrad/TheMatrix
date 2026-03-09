using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityHouseholdIndependenceAutonomyDecision(
        PersonId ResidentId,
        HouseholdId SourceHouseholdId);
}
