using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Enums;
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
            ICityPopulationPersonReadRepository personReadRepository,
            IHouseholdWriteRepository householdWriteRepository,
            ICityPopulationHouseholdFinancialStressStateRepository householdFinancialStressStateRepository,
            ICityPopulationEmployerFinancialStressStateRepository employerFinancialStressStateRepository,
            ICityPopulationAnchorCatalogRepository cityPopulationAnchorCatalogRepository,
            ICityHealthcarePressureSnapshotRepository healthcarePressureSnapshotRepository,
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
                employerStressByWorkplaceId =
                    (await employerFinancialStressStateRepository.ListByCityAsync(
                        cityId: cityId,
                        cancellationToken: cancellationToken))
                   .ToDictionary(x => x.WorkplaceId);
            IReadOnlyList<CityPopulationAnchorCatalogItem> workplaceAnchors =
                await cityPopulationAnchorCatalogRepository.ListByCityAsync(
                    cityId: cityId,
                    type: CityAnchorType.Workplace,
                    cancellationToken: cancellationToken);
            IReadOnlyList<CityPopulationAnchorCatalogItem> schoolAnchors =
                await cityPopulationAnchorCatalogRepository.ListByCityAsync(
                    cityId: cityId,
                    type: CityAnchorType.School,
                    cancellationToken: cancellationToken);
            IReadOnlyList<CityPopulationAnchorCatalogItem> hospitalAnchors =
                await cityPopulationAnchorCatalogRepository.ListByCityAsync(
                    cityId: cityId,
                    type: CityAnchorType.Hospital,
                    cancellationToken: cancellationToken);
            ClassicCityHealthcarePressureSnapshot? healthcarePressureSnapshot =
                await healthcarePressureSnapshotRepository.GetByCityAsync(
                    cityId,
                    cancellationToken);
            CityPopulationHealthcarePressureProfile healthcarePressureProfile =
                healthcarePressureSnapshot?.Pressure ?? CityPopulationHealthcarePressureProfile.Baseline;
            IReadOnlyCollection<HouseholdEntity> households =
                await householdWriteRepository.ListByCityAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            var householdsById = households.ToDictionary(
                keySelector: x => x.Id,
                elementSelector: x => x);
            Dictionary<EducationLevel, List<CityEducationInstitutionBinding>> institutionPools =
                ResidentPlacementPoolBuilder.BuildEducationInstitutionPools(residents);
            Dictionary<string, List<Job>> workplacePools =
                ResidentPlacementPoolBuilder.BuildWorkplacePools(residents);

            return new AdvanceCityPopulationWorkingSet(
                Residents: residents,
                ResidentsById: residentsById,
                ResidentsByHouseholdId: residentsByHouseholdId,
                Households: households,
                HouseholdsById: householdsById,
                Placements: placements,
                HousingByHouseholdId: housingByHouseholdId,
                DistrictByHouseholdId: districtByHouseholdId,
                ResidentialBuildingByHouseholdId: residentialBuildingByHouseholdId,
                FinancialStressByHouseholdId: financialStressByHouseholdId,
                EmployerStressByWorkplaceId: employerStressByWorkplaceId,
                WorkplaceAnchors: workplaceAnchors,
                SchoolAnchors: schoolAnchors,
                HospitalAnchors: hospitalAnchors,
                HealthcarePressureProfile: healthcarePressureProfile,
                InstitutionPools: institutionPools,
                WorkplacePools: workplacePools);
        }
    }
}
