using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Entities;

namespace Matrix.Population.Infrastructure.Persistence.Repositories
{
    public class PersonWriteRepository(PopulationDbContext context) : IPersonWriteRepository
    {
        private readonly PopulationDbContext _dbContext = context;

        public async Task AddAsync(Person person, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task AddRangeAsync(IReadOnlyList<Person> persons, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
        {
            _dbContext.Persons.RemoveRange(_dbContext.Persons);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Person person, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateAsync(Person person, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateRangeAsync(IReadOnlyList<Person> persons, CancellationToken cancellationToken = default)
        {
            const int batchSize = 1_000; // tune if needed

            for (int i = 0; i < persons.Count; i += batchSize)
            {
                var batch = persons
                    .Skip(i)
                    .Take(batchSize)
                    .ToList();

                await _dbContext.Persons.AddRangeAsync(batch, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Important: clear change tracker to avoid memory leak
                _dbContext.ChangeTracker.Clear();
            }
        }
    }
}
