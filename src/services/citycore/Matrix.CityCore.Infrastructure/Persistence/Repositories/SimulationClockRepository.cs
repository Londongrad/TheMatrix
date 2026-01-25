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
            return dbContext.SimulationClocks.SingleOrDefaultAsync(
                predicate: x => x.Id == cityId,
                cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<CityId>> ListActiveRunningCityIdsAsync(CancellationToken cancellationToken)
        {
            return await dbContext.SimulationClocks
               .AsNoTracking()
               .Where(clock => clock.State == ClockState.Running)
               .Join(
                    inner: dbContext.Cities.AsNoTracking()
                       .Where(city => city.Status == CityStatus.Active),
                    outerKeySelector: clock => clock.Id,
                    innerKeySelector: city => city.Id,
                    resultSelector: (
                        clock,
                        city) => clock.Id)
               .ToListAsync(cancellationToken);
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

        public async Task DeleteByCityIdAsync(
            CityId cityId,
            CancellationToken cancellationToken)
        {
            SimulationClock? clock = await dbContext.SimulationClocks.SingleOrDefaultAsync(
                predicate: x => x.Id == cityId,
                cancellationToken: cancellationToken);

            if (clock is null)
                return;

            dbContext.SimulationClocks.Remove(clock);
        }
    }
}
