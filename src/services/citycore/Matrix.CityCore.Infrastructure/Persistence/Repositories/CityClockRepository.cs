using Matrix.CityCore.Application.Abstractions;
using Matrix.CityCore.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace Matrix.CityCore.Infrastructure.Persistence.Repositories
{
    public sealed class CityClockRepository(CityCoreDbContext dbContext) : ICityClockRepository
    {
        private readonly CityCoreDbContext _dbContext = dbContext;

        public async Task<CityClock?> GetAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.CityClocks.SingleOrDefaultAsync(cancellationToken);
        }

        public void Add(CityClock clock)
        {
            _dbContext.CityClocks.Add(clock);
        }
    }
}
