using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityPopulationPersonReadRepository
    {
        Task<IReadOnlyCollection<Person>> ListByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Person>> ListByCityAndIdsAsync(
            CityId cityId,
            IReadOnlyCollection<PersonId> personIds,
            CancellationToken cancellationToken = default);

        Task<(IReadOnlyCollection<Person> Items, int TotalCount)> GetPageByCityAsync(
            CityId cityId,
            Pagination pagination,
            CancellationToken cancellationToken = default);

        Task<Person?> FindByCityAndPersonIdAsync(
            CityId cityId,
            PersonId personId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Person>> ListChildrenByParentIdAsync(
            CityId cityId,
            PersonId parentId,
            CancellationToken cancellationToken = default);

        Task<CityId?> FindCityIdByPersonIdAsync(
            PersonId personId,
            CancellationToken cancellationToken = default);

        Task<CityResidentHousingSnapshot?> FindHousingSnapshotByPersonIdAsync(
            CityId cityId,
            PersonId personId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<HouseholdId, HousingStatus>> ListHousingStatusesByHouseholdAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<CityEmploymentWorkplaceSnapshot>> ListEmploymentWorkplacesAsync(
            CityId cityId,
            CancellationToken cancellationToken = default);

        Task<CityEmploymentWorkplaceSnapshot?> FindEmploymentWorkplaceByIdAsync(
            CityId cityId,
            WorkplaceId workplaceId,
            CancellationToken cancellationToken = default);
    }
}
