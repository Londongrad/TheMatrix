using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories
{
    public class PersonWriteRepository(PopulationDbContext context) : IPersonWriteRepository
    {
        private readonly PopulationDbContext _dbContext = context;

        public async Task AddAsync(
            Person person,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.Persons.AddAsync(
                entity: person,
                cancellationToken: cancellationToken);
        }

        public async Task AddRangeAsync(
            IReadOnlyCollection<Person> persons,
            CancellationToken cancellationToken = default)
        {
            await _dbContext.Persons.AddRangeAsync(
                entities: persons,
                cancellationToken: cancellationToken);
        }

        public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
        {
            await _dbContext.Persons.ExecuteDeleteAsync(cancellationToken);
        }

        public Task DeleteAsync(
            Person person,
            CancellationToken cancellationToken = default)
        {
            _dbContext.Persons.Remove(person);
            return Task.CompletedTask;
        }

        public async Task<IReadOnlyCollection<Person>> ListByCityAsync(
            CityId cityId,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Persons
               .Join(
                    inner: _dbContext.Households.Where(x => x.CityId == cityId),
                    outerKeySelector: person => person.HouseholdId,
                    innerKeySelector: household => household.Id,
                    resultSelector: (
                        person,
                        _) => person)
               .ToListAsync(cancellationToken);
        }

        public Task UpdateAsync(
            Person person,
            CancellationToken cancellationToken = default)
        {
            _dbContext.Persons.Update(person);
            return Task.CompletedTask;
        }
    }
}
