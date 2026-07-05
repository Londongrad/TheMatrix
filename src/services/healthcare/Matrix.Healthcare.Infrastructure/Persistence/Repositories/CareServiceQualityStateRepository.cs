using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Infrastructure.Persistence.Repositories;

public sealed class CareServiceQualityStateRepository(HealthcareDbContext dbContext)
    : ICareServiceQualityStateRepository
{
    public Task<CareServiceQualityState?> GetAsync(
        SimulationHostId simulationHostId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.CareServiceQualityStates.FindAsync(
            keyValues: [simulationHostId],
            cancellationToken: cancellationToken).AsTask();
    }

    public Task AddAsync(
        CareServiceQualityState state,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        return dbContext.CareServiceQualityStates.AddAsync(
            state,
            cancellationToken).AsTask();
    }
}
