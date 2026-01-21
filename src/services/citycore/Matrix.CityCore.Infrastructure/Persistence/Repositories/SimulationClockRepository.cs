using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Simulation;
using Microsoft.EntityFrameworkCore;

namespace Matrix.CityCore.Infrastructure.Persistence.Repositories
{
    public sealed class SimulationClockRepository(CityCoreDbContext dbContext) : ISimulationClockRepository
    {
        public Task<SimulationClock?> GetByCityIdAsync(
            CityId cityId,
            CancellationToken cancellationToken)
        {
            return dbContext.SimulationClocks.FirstOrDefaultAsync(
                predicate: x => x.Id == cityId,
                cancellationToken: cancellationToken);
        }

        public Task AddAsync(
            SimulationClock clock,
            CancellationToken cancellationToken)
        {
            return dbContext.SimulationClocks.AddAsync(
                    entity: clock,
                    cancellationToken: cancellationToken)
               .AsTask();
        }

        public Task DeleteByCityIdAsync(
            CityId cityId,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
