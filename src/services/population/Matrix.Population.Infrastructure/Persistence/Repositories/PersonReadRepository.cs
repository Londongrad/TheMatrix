using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
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

        public async Task<Person?> FindByIdAsync(PersonId id, CancellationToken cancellationToken = default)
        {
            return await _dbContext
                .Persons
                .FirstOrDefaultAsync(predicate: p => p.Id == id, cancellationToken: cancellationToken);
        }

        public async Task<(IReadOnlyCollection<Person> Items, int TotalCount)> GetPageAsync(
            Pagination pagination,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Person> query = _dbContext.Persons.AsNoTracking();

            int totalCount = await query.CountAsync(cancellationToken);

            List<Person> items = await query
                .OrderBy(p => p.Id)
                .Skip(pagination.Skip)
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
