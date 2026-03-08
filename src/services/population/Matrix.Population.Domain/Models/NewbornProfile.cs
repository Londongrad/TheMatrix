using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Models
{
    public sealed record NewbornProfile(
        PersonId PersonId,
        PersonName Name,
        Sex Sex,
        Personality Personality,
        HealthLevel Health,
        BodyWeight Weight);
}
