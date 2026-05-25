using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using PersonEntity = Matrix.Population.Domain.Entities.Person;
using HouseholdEntity = Matrix.Population.Domain.Entities.Household;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal sealed record AdvanceCityPopulationWorkingSet(
        List<PersonEntity> Residents,
        Dictionary<PersonId, PersonEntity> ResidentsById,
        Dictionary<HouseholdId, IReadOnlyCollection<PersonEntity>> ResidentsByHouseholdId,
        IReadOnlyCollection<HouseholdEntity> Households,
        Dictionary<HouseholdId, HouseholdEntity> HouseholdsById,
        IReadOnlyCollection<ClassicCityHouseholdPlacement> Placements,
        IReadOnlyDictionary<HouseholdId, HousingStatus> HousingByHouseholdId,
        IReadOnlyDictionary<HouseholdId, DistrictId?> DistrictByHouseholdId,
        IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> ResidentialBuildingByHouseholdId,
        IReadOnlyDictionary<HouseholdId, CityPopulationHouseholdFinancialStressState> FinancialStressByHouseholdId,
        IReadOnlyDictionary<WorkplaceId, CityPopulationEmployerFinancialStressState> EmployerStressByWorkplaceId,
        IReadOnlyList<CityPopulationAnchorCatalogItem> WorkplaceAnchors,
        IReadOnlyList<CityPopulationAnchorCatalogItem> SchoolAnchors,
        IReadOnlyList<CityPopulationAnchorCatalogItem> HospitalAnchors,
        CityPopulationHealthcarePressureProfile HealthcarePressureProfile,
        Dictionary<EducationLevel, List<CityEducationInstitutionBinding>> InstitutionPools,
        Dictionary<string, List<Job>> WorkplacePools);
}
