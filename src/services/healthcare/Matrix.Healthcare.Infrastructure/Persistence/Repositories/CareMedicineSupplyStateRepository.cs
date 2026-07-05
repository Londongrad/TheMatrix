using Matrix.Healthcare.Application.Abstractions;
using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Infrastructure.Persistence.Repositories;

public sealed class CareMedicineSupplyStateRepository(HealthcareDbContext dbContext)
    : ICareMedicineSupplyStateRepository
{
    public Task<CareMedicineSupplyState?> GetAsync(
        SimulationHostId simulationHostId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.CareMedicineSupplyStates.FindAsync(
            keyValues: [simulationHostId],
            cancellationToken: cancellationToken).AsTask();
    }

    public Task AddAsync(
        CareMedicineSupplyState state,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        return dbContext.CareMedicineSupplyStates.AddAsync(
            state,
            cancellationToken).AsTask();
    }
}
