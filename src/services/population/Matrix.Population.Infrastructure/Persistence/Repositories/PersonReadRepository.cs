using Matrix.BuildingBlocks.Application.Models;
using Matrix.BuildingBlocks.Infrastructure.Exceptions;
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

        public async Task<Person> GetByIdAsync(PersonId id, CancellationToken cancellationToken = default)
        {
            return await _dbContext
                .Persons
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
                ?? throw new NotFoundException(nameof(Person), id);
        }

        public async Task<(IReadOnlyCollection<Person> Items, int TotalCount)> GetPageAsync(
            Pagination pagination,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Persons.AsNoTracking();

            int totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(p => p.Id)
                .Skip(pagination.Skip)
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
