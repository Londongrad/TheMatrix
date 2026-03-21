using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationCore.Infrastructure.Persistence.Repositories.Scenarios.ClassicCity
{
    public sealed class ClassicCitySimulationHostReadRepository(SimulationCoreDbContext dbContext)
        : ISimulationHostReadRepository
    {
        public async Task<SimulationHost?> GetBySimulationIdAsync(
            SimulationId simulationId,
            CancellationToken cancellationToken)
        {
            CityId cityId = new(simulationId.Value);

            var projection = await dbContext.Cities
               .AsNoTracking()
               .Where(city => city.Id == cityId)
               .Select(city => new
                {
                    CityId = city.Id,
                    city.SimulationKind,
                    city.Status,
                    city.CreatedAtUtc,
                    city.ArchivedAtUtc
                })
               .SingleOrDefaultAsync(cancellationToken);

            return projection is null
                ? null
                : new SimulationHost(
                    SimulationId: simulationId,
                    HostId: new SimulationHostId(projection.CityId.Value),
                    HostKind: SimulationHostKind.City,
                    SimulationKind: projection.SimulationKind,
                    State: MapState(projection.Status),
                    CreatedAtUtc: projection.CreatedAtUtc,
                    ArchivedAtUtc: projection.ArchivedAtUtc);
        }

        private static SimulationHostState MapState(CityStatus status)
        {
            return status switch
            {
                CityStatus.Active => SimulationHostState.Active,
                CityStatus.Archived => SimulationHostState.Archived,
                CityStatus.Provisioning => SimulationHostState.Provisioning,
                CityStatus.ProvisioningFailed => SimulationHostState.ProvisioningFailed,
                _ => throw new ArgumentOutOfRangeException(
                    paramName: nameof(status),
                    actualValue: status,
                    message: "Unsupported classic city status.")
            };
        }
    }
}
