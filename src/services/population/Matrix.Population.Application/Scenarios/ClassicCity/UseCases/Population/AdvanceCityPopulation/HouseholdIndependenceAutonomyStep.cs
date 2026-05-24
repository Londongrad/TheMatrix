using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class HouseholdIndependenceAutonomyStep
    {
        internal static async Task<int> ApplyAsync(
            CityId cityId,
            IReadOnlyDictionary<PersonId, PersonEntity> residentsById,
            DateOnly previousDate,
            DateOnly currentDate,
            IHouseholdWriteRepository householdWriteRepository,
            CityHouseholdIndependenceAutonomyPolicy householdIndependenceAutonomyPolicy,
            ICollection<CityPopulationActivityWriteModel> activityEntries,
            DateTimeOffset occurredAtUtc,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<ClassicCityHouseholdPlacement> placements =
                await householdWriteRepository.ListPlacementsByCityAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);

            if (placements.Count == 0)
                return 0;

            var housingStatuses = placements.ToDictionary(
                keySelector: x => x.HouseholdId,
                elementSelector: x => x.HousingStatus);

            IReadOnlyList<CityHouseholdIndependenceAutonomyDecision> decisions =
                householdIndependenceAutonomyPolicy.Plan(
                    residents: residentsById.Values.ToArray(),
                    housingStatuses: housingStatuses,
                    previousDate: previousDate,
                    currentDate: currentDate);

            if (decisions.Count == 0)
                return 0;

            int affectedResidents = 0;

            foreach (CityHouseholdIndependenceAutonomyDecision decision in decisions)
            {
                if (!residentsById.TryGetValue(
                        key: decision.ResidentId,
                        value: out PersonEntity? resident) ||
                    resident.HouseholdId != decision.SourceHouseholdId)
                    continue;

                if (!await ClassicCityHouseholdAutonomySupport.MoveResidentIntoIndependentHouseholdAsync(
                        cityId: cityId,
                        resident: resident,
                        householdWriteRepository: householdWriteRepository,
                        createdAtUtc: occurredAtUtc,
                        cancellationToken: cancellationToken))
                    continue;

                activityEntries.Add(
                    ClassicCityActivityFactory.ResidentFormedIndependentHousehold(
                        cityId: cityId.Value,
                        currentDate: currentDate,
                        resident: resident,
                        source: CityPopulationActivitySource.Autonomy,
                        occurredAtUtc: occurredAtUtc));
                affectedResidents++;
            }

            return affectedResidents;
        }
    }
}
