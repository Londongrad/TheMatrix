using Matrix.Population.Application.Integration;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using HouseholdEntity = Matrix.Population.Domain.Entities.Household;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal sealed record AdvanceCityPopulationWorkingSet(
        List<PersonEntity> Residents,
        Dictionary<PersonId, PersonEntity> ResidentsById,
        Dictionary<HouseholdId, IReadOnlyCollection<PersonEntity>> ResidentsByHouseholdId,
        IReadOnlyDictionary<PersonId, ResidentExternalActivityProfile> ExternalActivitiesByResidentId,
        IReadOnlyDictionary<PersonId, PersonRoutineProfile> RoutineProfilesByResidentId,
        IReadOnlyDictionary<PersonId, CityResidentEconomicContext> EconomicContextsByResidentId,
        IReadOnlyCollection<HouseholdEntity> Households,
        Dictionary<HouseholdId, HouseholdEntity> HouseholdsById,
        IReadOnlyCollection<ClassicCityHouseholdPlacement> Placements,
        IReadOnlyDictionary<HouseholdId, HousingStatus> HousingByHouseholdId,
        IReadOnlyDictionary<HouseholdId, DistrictId?> DistrictByHouseholdId,
        IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> ResidentialBuildingByHouseholdId,
        IReadOnlyDictionary<HouseholdId, CityPopulationHouseholdFinancialStressState> FinancialStressByHouseholdId,
        IReadOnlyDictionary<WorkplaceId, CityPopulationEmployerFinancialStressState> EmployerStressByWorkplaceId,
        IReadOnlyList<CityPopulationAnchorCatalogItem> WorkplaceAnchors,
        Dictionary<string, List<Job>> WorkplacePools);
}
