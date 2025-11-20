using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories
{
    public class PersonWriteRepository(PopulationDbContext context) : IPersonWriteRepository
    {
        private readonly PopulationDbContext _dbContext = context;

        public async Task AddAsync(Person person, CancellationToken cancellationToken = default)
        {
            await _dbContext.Persons.AddAsync(person, cancellationToken);
        }

        public async Task AddRangeAsync(IReadOnlyCollection<Person> persons, CancellationToken cancellationToken = default)
        {
            await _dbContext.Persons.AddRangeAsync(persons, cancellationToken);
        }

        public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
        {
            await _dbContext.Persons.ExecuteDeleteAsync(cancellationToken);
        }

        public Task DeleteAsync(Person person, CancellationToken cancellationToken = default)
        {
            _dbContext.Persons.Remove(person);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Person person, CancellationToken cancellationToken = default)
        {
            _dbContext.Persons.Update(person);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
