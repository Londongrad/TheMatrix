using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Services;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class ResidentNeedsProgressionStep
    {
        internal static ResidentProgressionStepResult Apply(
            PersonEntity person,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc,
            CityPopulationEnvironment? environment,
            PersonRoutineProfile routineProfile,
            PersonNeedsProgressionPolicy personNeedsProgressionPolicy)
        {
            int utcOffsetMinutes = environment?.UtcOffsetMinutes ?? 0;
            PersonNeedsProgressionEffect effect = personNeedsProgressionPolicy.Calculate(
                person: person,
                fromSimTimeUtc: fromSimTimeUtc,
                toSimTimeUtc: toSimTimeUtc,
                utcOffsetMinutes: utcOffsetMinutes,
                routineProfile: routineProfile);
            bool changed = person.ApplyNeedsProgression(
                effect: effect);

            return new ResidentProgressionStepResult(
                PopulationChanged: changed,
                ExternalHealthDelta: effect.HealthDelta);
        }
    }
}
