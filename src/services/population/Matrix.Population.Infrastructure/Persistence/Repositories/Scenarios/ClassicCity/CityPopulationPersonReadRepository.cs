using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
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
               .AsNoTracking()
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
    }
}
