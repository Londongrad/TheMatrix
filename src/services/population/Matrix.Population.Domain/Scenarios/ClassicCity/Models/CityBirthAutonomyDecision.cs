using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityBirthAutonomyDecision(
        PersonId MotherId,
        PersonId? FatherId,
        NewbornProfile Newborn);
}
