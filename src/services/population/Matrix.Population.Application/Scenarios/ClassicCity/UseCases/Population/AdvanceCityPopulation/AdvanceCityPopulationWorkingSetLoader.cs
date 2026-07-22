using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Integration;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using HouseholdEntity = Matrix.Population.Domain.Entities.Household;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class AdvanceCityPopulationWorkingSetLoader
    {
        internal static async Task<AdvanceCityPopulationWorkingSet> LoadAsync(
            CityId cityId,
            DateOnly currentDate,
            ICityPopulationPersonReadRepository personReadRepository,
            IHouseholdWriteRepository householdWriteRepository,
            ICityPopulationHouseholdFinancialStressStateRepository householdFinancialStressStateRepository,
            ICityPopulationEmployerFinancialStressStateRepository employerFinancialStressStateRepository,
            ICityPopulationAnchorCatalogRepository cityPopulationAnchorCatalogRepository,
            IResidentExternalActivityProfileReader externalActivityProfileReader,
            ResidentExternalActivityReadScope externalActivityReadScope,
            bool includeEmploymentContext,
            bool includeEconomicContexts,
            CancellationToken cancellationToken)
        {
            var residents = (await personReadRepository.ListByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken)).ToList();
            var residentsById = residents.ToDictionary(
                keySelector: x => x.Id,
                elementSelector: x => x);
            var residentsByHouseholdId = residents
               .GroupBy(x => x.HouseholdId)
               .ToDictionary(
                    keySelector: x => x.Key,
                    elementSelector: x => (IReadOnlyCollection<PersonEntity>)x.ToList());
            var routineProfilesByResidentId = new Dictionary<PersonId, PersonRoutineProfile>(residents.Count);
            IReadOnlyDictionary<PersonId, ResidentExternalActivityProfile> externalActivitiesByResidentId =
                await externalActivityProfileReader.ReadAsync(
                    simulationHostId: cityId.Value,
                    residents: residents,
                    scope: externalActivityReadScope,
                    cancellationToken: cancellationToken);
            var economicContextsByResidentId =
                new Dictionary<PersonId, CityResidentEconomicContext>(
                    includeEconomicContexts ? residents.Count : 0);
            foreach (PersonEntity resident in residents)
            {
                ResidentExternalActivityProfile externalActivity = externalActivitiesByResidentId.TryGetValue(
                    key: resident.Id,
                    value: out ResidentExternalActivityProfile? resolvedActivity)
                    ? resolvedActivity
                    : ResidentExternalActivityProfile.None;
                routineProfilesByResidentId[resident.Id] = PersonRoutineProfileFactory.Create(
                    resident: resident,
                    externalActivity: externalActivity);
                if (includeEconomicContexts)
                    economicContextsByResidentId[resident.Id] = CityResidentEconomicContextFactory.Create(
                        resident: resident,
                        externalActivity: externalActivity,
                        currentDate: currentDate);
            }
            IReadOnlyCollection<ClassicCityHouseholdPlacement> placements =
                await householdWriteRepository.ListPlacementsByCityAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId = placements
               .ToDictionary(
                    keySelector: x => x.HouseholdId,
                    elementSelector: x => x.HousingStatus);
            IReadOnlyDictionary<HouseholdId, DistrictId?> districtByHouseholdId = placements
               .ToDictionary(
                    keySelector: x => x.HouseholdId,
                    elementSelector: x => x.DistrictId);
            IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> residentialBuildingByHouseholdId =
                placements.ToDictionary(
                    keySelector: x => x.HouseholdId,
                    elementSelector: x => x.ResidentialBuildingId);
            IReadOnlyDictionary<HouseholdId, CityPopulationHouseholdFinancialStressState>
                financialStressByHouseholdId =
                    (await householdFinancialStressStateRepository.ListByCityAsync(
                        cityId: cityId,
                        cancellationToken: cancellationToken))
                   .ToDictionary(x => x.HouseholdId);
            IReadOnlyDictionary<WorkplaceId, CityPopulationEmployerFinancialStressState>
                employerStressByWorkplaceId = includeEmploymentContext
                    ? (await employerFinancialStressStateRepository.ListByCityAsync(
                        cityId: cityId,
                        cancellationToken: cancellationToken))
                       .ToDictionary(x => x.WorkplaceId)
                    : new Dictionary<WorkplaceId, CityPopulationEmployerFinancialStressState>();
            IReadOnlyList<CityPopulationAnchorCatalogItem> workplaceAnchors = includeEmploymentContext
                ? await cityPopulationAnchorCatalogRepository.ListByCityAsync(
                    cityId: cityId,
                    type: CityAnchorType.Workplace,
                    cancellationToken: cancellationToken)
                : [];
            IReadOnlyCollection<HouseholdEntity> households =
                await householdWriteRepository.ListByCityAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            var householdsById = households.ToDictionary(
                keySelector: x => x.Id,
                elementSelector: x => x);
            Dictionary<string, List<Job>> workplacePools = includeEmploymentContext
                ? ResidentPlacementPoolBuilder.BuildWorkplacePools(residents)
                : [];

            return new AdvanceCityPopulationWorkingSet(
                Residents: residents,
                ResidentsById: residentsById,
                ResidentsByHouseholdId: residentsByHouseholdId,
                ExternalActivitiesByResidentId: externalActivitiesByResidentId,
                RoutineProfilesByResidentId: routineProfilesByResidentId,
                EconomicContextsByResidentId: economicContextsByResidentId,
                Households: households,
                HouseholdsById: householdsById,
                Placements: placements,
                HousingByHouseholdId: housingByHouseholdId,
                DistrictByHouseholdId: districtByHouseholdId,
                ResidentialBuildingByHouseholdId: residentialBuildingByHouseholdId,
                FinancialStressByHouseholdId: financialStressByHouseholdId,
                EmployerStressByWorkplaceId: employerStressByWorkplaceId,
                WorkplaceAnchors: workplaceAnchors,
                WorkplacePools: workplacePools);
        }
    }
}
