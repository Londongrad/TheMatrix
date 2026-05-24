using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.Common;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.ValueObjects;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class CivilRegistryAutonomyStep
    {
        internal static async Task<int> ApplyAsync(
            CityId cityId,
            IReadOnlyDictionary<PersonId, PersonEntity> residentsById,
            DateOnly previousDate,
            DateOnly currentDate,
            IHouseholdWriteRepository householdWriteRepository,
            MarriageDomainService marriageDomainService,
            CityCivilRegistryAutonomyPolicy civilRegistryAutonomyPolicy,
            ICollection<CityPopulationActivityWriteModel> activityEntries,
            DateTimeOffset occurredAtUtc,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityCivilRegistryAutonomyDecision> decisions = civilRegistryAutonomyPolicy.Plan(
                residents: residentsById.Values.ToArray(),
                previousDate: previousDate,
                currentDate: currentDate);
            if (decisions.Count == 0)
                return 0;
            int affectedResidents = 0;
            foreach (CityCivilRegistryAutonomyDecision decision in decisions)
            {
                if (!residentsById.TryGetValue(
                        key: decision.FirstResidentId,
                        value: out PersonEntity? firstResident) ||
                    !residentsById.TryGetValue(
                        key: decision.SecondResidentId,
                        value: out PersonEntity? secondResident))
                    continue;
                switch (decision.Type)
                {
                    case CityCivilRegistryAutonomyDecisionType.Marriage:
                        marriageDomainService.RegisterMarriage(
                            person: firstResident,
                            spouse: secondResident,
                            currentDate: currentDate);
                        await ClassicCityCivilRegistryHouseholdSupport.MergeSpousesIntoSharedHouseholdAsync(
                            cityId: cityId,
                            firstResident: firstResident,
                            secondResident: secondResident,
                            householdWriteRepository: householdWriteRepository,
                            cancellationToken: cancellationToken);
                        activityEntries.Add(
                            ClassicCityActivityFactory.ResidentsMarried(
                                cityId: cityId.Value,
                                currentDate: currentDate,
                                firstResident: firstResident,
                                secondResident: secondResident,
                                source: CityPopulationActivitySource.Autonomy,
                                occurredAtUtc: occurredAtUtc));
                        affectedResidents += 2;
                        break;
                    case CityCivilRegistryAutonomyDecisionType.Divorce:
                        if (firstResident.SpouseId != secondResident.Id || secondResident.SpouseId != firstResident.Id)
                            continue;
                        marriageDomainService.RegisterDivorce(
                            person: firstResident,
                            spouse: secondResident,
                            currentDate: currentDate);
                        await ClassicCityCivilRegistryHouseholdSupport.SeparateDivorcedSpousesAsync(
                            cityId: cityId,
                            firstResident: firstResident,
                            secondResident: secondResident,
                            householdWriteRepository: householdWriteRepository,
                            createdAtUtc: occurredAtUtc,
                            cancellationToken: cancellationToken);
                        activityEntries.Add(
                            ClassicCityActivityFactory.ResidentsDivorced(
                                cityId: cityId.Value,
                                currentDate: currentDate,
                                firstResident: firstResident,
                                secondResident: secondResident,
                                source: CityPopulationActivitySource.Autonomy,
                                occurredAtUtc: occurredAtUtc));
                        affectedResidents += 2;
                        break;
                }
            }

            return affectedResidents;
        }
    }
}
