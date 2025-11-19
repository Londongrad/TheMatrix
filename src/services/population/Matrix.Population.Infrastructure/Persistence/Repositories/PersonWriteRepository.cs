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
            _dbContext.Persons.Add(person);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task AddRangeAsync(IReadOnlyCollection<Person> persons, CancellationToken cancellationToken = default)
        {
            await _dbContext.Persons.AddRangeAsync(persons, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
        {
            await _dbContext.Persons.ExecuteDeleteAsync(cancellationToken);
        }

        public async Task DeleteAsync(Person person, CancellationToken cancellationToken = default)
        {
            _dbContext.Persons.Remove(person);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Person person, CancellationToken cancellationToken = default)
        {
            _dbContext.Update(person);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateRangeAsync(IReadOnlyCollection<Person> persons, CancellationToken cancellationToken = default)
        {
            const int batchSize = 1_000; // tune if needed

            for (int i = 0; i < persons.Count; i += batchSize)
            {
                var batch = persons
                    .Skip(i)
                    .Take(batchSize)
                    .ToList();

                _dbContext.Persons.UpdateRange(batch);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Important: clear change tracker to avoid memory leak
                _dbContext.ChangeTracker.Clear();
            }
        }
    }
}
