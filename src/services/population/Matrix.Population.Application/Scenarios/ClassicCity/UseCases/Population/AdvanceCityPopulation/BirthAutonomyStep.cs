using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using HouseholdEntity = Matrix.Population.Domain.Entities.Household;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class BirthAutonomyStep
    {
        internal static async Task<int> ApplyAsync(
            CityId cityId,
            IDictionary<PersonId, PersonEntity> residentsById,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingStatusesByHouseholdId,
            DateOnly previousDate,
            DateOnly currentDate,
            CityBirthAutonomyPolicy birthAutonomyPolicy,
            PopulationBirthDomainService populationBirthDomainService,
            IPersonWriteRepository personWriteRepository,
            IHouseholdWriteRepository householdWriteRepository,
            ICollection<CityPopulationActivityWriteModel> activityEntries,
            ICollection<PersonEntity> residents,
            DateTimeOffset occurredAtUtc,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityBirthAutonomyDecision> decisions = birthAutonomyPolicy.Plan(
                residents: residentsById.Values.ToArray(),
                housingStatuses: housingStatusesByHouseholdId,
                previousDate: previousDate,
                currentDate: currentDate);

            if (decisions.Count == 0)
                return 0;

            int affectedResidents = 0;

            foreach (CityBirthAutonomyDecision decision in decisions)
            {
                if (!residentsById.TryGetValue(
                        key: decision.MotherId,
                        value: out PersonEntity? mother))
                    continue;

                PersonEntity? father = null;
                if (decision.FatherId is not null &&
                    !residentsById.TryGetValue(
                        key: decision.FatherId.Value,
                        value: out father))
                    continue;

                HouseholdEntity? household = await householdWriteRepository.FindByIdAsync(
                    householdId: mother.HouseholdId,
                    cancellationToken: cancellationToken);

                if (household is null)
                    continue;

                PersonEntity newborn = populationBirthDomainService.RegisterBirth(
                    mother: mother,
                    father: father,
                    household: household,
                    newborn: decision.Newborn,
                    currentDate: currentDate);

                await personWriteRepository.AddAsync(
                    person: newborn,
                    cancellationToken: cancellationToken);
                await householdWriteRepository.UpdateAsync(
                    household: household,
                    cancellationToken: cancellationToken);

                residents.Add(newborn);
                residentsById[newborn.Id] = newborn;
                activityEntries.Add(
                    ClassicCityActivityFactory.ResidentBorn(
                        cityId: cityId.Value,
                        currentDate: currentDate,
                        resident: newborn,
                        mother: mother,
                        father: father,
                        source: CityPopulationActivitySource.Autonomy,
                        occurredAtUtc: occurredAtUtc));
                affectedResidents++;
            }

            return affectedResidents;
        }
    }
}
