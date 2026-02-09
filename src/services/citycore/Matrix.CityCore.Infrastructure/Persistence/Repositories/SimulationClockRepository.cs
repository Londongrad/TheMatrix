using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Simulation;
using Microsoft.EntityFrameworkCore;

namespace Matrix.CityCore.Infrastructure.Persistence.Repositories
{
    public sealed class SimulationClockRepository(CityCoreDbContext dbContext) : ISimulationClockRepository
    {
        public Task<SimulationClock?> GetBySimulationIdAsync(
            SimulationId simulationId,
            CancellationToken cancellationToken)
        {
            CityId cityId = new(simulationId.Value);

            return dbContext.SimulationClocks.SingleOrDefaultAsync(
                predicate: x => x.Id == cityId,
                cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<SimulationId>> ListActiveRunningSimulationIdsAsync(
            CancellationToken cancellationToken)
        {
            List<CityId> cityIds = await dbContext.SimulationClocks
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

            return cityIds
               .Select(cityId => new SimulationId(cityId.Value))
               .ToArray();
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

        public async Task DeleteBySimulationIdAsync(
            SimulationId simulationId,
            CancellationToken cancellationToken)
        {
            CityId cityId = new(simulationId.Value);

            SimulationClock? clock = await dbContext.SimulationClocks.SingleOrDefaultAsync(
                predicate: x => x.Id == cityId,
                cancellationToken: cancellationToken);

            if (clock is null)
                return;

            dbContext.SimulationClocks.Remove(clock);
        }
    }
}
