using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public enum CityCivilRegistryAutonomyDecisionType
    {
        Marriage = 1,
        Divorce = 2
    }

    public sealed record CityCivilRegistryAutonomyDecision(
        CityCivilRegistryAutonomyDecisionType Type,
        PersonId FirstResidentId,
        PersonId SecondResidentId);
}
