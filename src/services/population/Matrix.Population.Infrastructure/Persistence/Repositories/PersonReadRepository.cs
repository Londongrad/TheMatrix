using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence.Repositories
{
    public sealed class PersonReadRepository(PopulationDbContext dbContext) : IPersonReadRepository
    {
        private readonly PopulationDbContext _dbContext = dbContext;

        public async Task<IReadOnlyCollection<Person>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext
                .Persons
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbContext
                .Persons
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id.Value == id, cancellationToken);
        }
    }
}
