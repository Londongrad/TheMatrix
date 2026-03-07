using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity
{
    public sealed class CityPopulationPersonReadRepository(PopulationDbContext dbContext)
        : ICityPopulationPersonReadRepository
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public async Task<IReadOnlyCollection<Person>> ListByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Persons
               .Join(
                    inner: _dbContext.ClassicCityHouseholdPlacements.Where(x => x.CityId == cityId),
                    outerKeySelector: person => person.HouseholdId,
                    innerKeySelector: placement => placement.HouseholdId,
                    resultSelector: (
                        person,
                        _) => person)
               .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyCollection<Person> Items, int TotalCount)> GetPageByCityAsync(
            CityId cityId,
            Pagination pagination,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Person> query = _dbContext.Persons
               .AsNoTracking()
               .Join(
                    inner: _dbContext.ClassicCityHouseholdPlacements.Where(x => x.CityId == cityId),
                    outerKeySelector: person => person.HouseholdId,
                    innerKeySelector: placement => placement.HouseholdId,
                    resultSelector: (
                        person,
                        _) => person);

            int totalCount = await query.CountAsync(cancellationToken);

            List<Person> items = await query
               .OrderBy(person => person.Id)
               .Skip(pagination.Skip)
               .Take(pagination.PageSize)
               .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<Person?> FindByCityAndPersonIdAsync(
            CityId cityId,
            PersonId personId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Persons
               .AsNoTracking()
               .Join(
                    inner: _dbContext.ClassicCityHouseholdPlacements.Where(x => x.CityId == cityId),
                    outerKeySelector: person => person.HouseholdId,
                    innerKeySelector: placement => placement.HouseholdId,
                    resultSelector: (
                        person,
                        _) => person)
               .FirstOrDefaultAsync(
                    predicate: person => person.Id == personId,
                    cancellationToken: cancellationToken);
        }

        public async Task<CityId?> FindCityIdByPersonIdAsync(
            PersonId personId,
            CancellationToken cancellationToken = default)
        {
            Guid? cityId = await (
                from person in _dbContext.Persons.AsNoTracking()
                join placement in _dbContext.ClassicCityHouseholdPlacements.AsNoTracking()
                    on person.HouseholdId equals placement.HouseholdId
                where person.Id == personId
                select (Guid?)placement.CityId.Value)
               .FirstOrDefaultAsync(cancellationToken);

            return cityId.HasValue
                ? CityId.From(cityId.Value)
                : null;
        }

        public async Task<CityResidentHousingSnapshot?> FindHousingSnapshotByPersonIdAsync(
            CityId cityId,
            PersonId personId,
            CancellationToken cancellationToken = default)
        {
            var snapshot = await (
                from person in _dbContext.Persons.AsNoTracking()
                join placement in _dbContext.ClassicCityHouseholdPlacements.AsNoTracking()
                    on person.HouseholdId equals placement.HouseholdId
                where placement.CityId == cityId && person.Id == personId
                select new
                {
                    HouseholdId = person.HouseholdId.Value,
                    placement.HousingStatus,
                    ResidentialBuildingId = placement.ResidentialBuildingId.HasValue
                        ? placement.ResidentialBuildingId.Value.Value
                        : (Guid?)null
                })
               .FirstOrDefaultAsync(cancellationToken);

            return snapshot is null
                ? null
                : new CityResidentHousingSnapshot(
                    HouseholdId: HouseholdId.From(snapshot.HouseholdId),
                    HousingStatus: snapshot.HousingStatus,
                    ResidentialBuildingId: snapshot.ResidentialBuildingId.HasValue
                        ? ResidentialBuildingId.From(snapshot.ResidentialBuildingId.Value)
                        : null);
        }
    }
}
