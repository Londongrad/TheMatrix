using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.Common;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Services;
using PersonEntity = Matrix.Population.Domain.Entities.Person;
using PersonId = Matrix.Population.Domain.ValueObjects.PersonId;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class ResidentNeedsProgressionStep
    {
        internal static bool Apply(
            PersonEntity person,
            IReadOnlyDictionary<PersonId, PersonEntity> residentsById,
            DateTimeOffset fromSimTimeUtc,
            DateTimeOffset toSimTimeUtc,
            DateOnly currentDate,
            CityPopulationEnvironment? environment,
            MarriageDomainService marriageDomainService,
            PersonNeedsProgressionPolicy personNeedsProgressionPolicy)
        {
            int utcOffsetMinutes = environment?.UtcOffsetMinutes ?? 0;
            PersonNeedsProgressionEffect effect = personNeedsProgressionPolicy.Calculate(
                person: person,
                fromSimTimeUtc: fromSimTimeUtc,
                toSimTimeUtc: toSimTimeUtc,
                utcOffsetMinutes: utcOffsetMinutes);
            bool wasAlive = person.IsAlive;
            bool changed = person.ApplyNeedsProgression(
                effect: effect,
                currentDate: currentDate);
            if (wasAlive && !person.IsAlive)
                changed = ClassicCityWidowhoodSupport.TryRegisterWidowhood(
                              deceased: person,
                              residentsById: residentsById,
                              marriageDomainService: marriageDomainService) ||
                          changed;
            return changed;
        }
    }
}
