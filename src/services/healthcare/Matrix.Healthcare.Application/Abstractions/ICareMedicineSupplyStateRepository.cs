using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Application.Abstractions;

public interface ICareMedicineSupplyStateRepository
{
    Task<CareMedicineSupplyState?> GetAsync(
        SimulationHostId simulationHostId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CareMedicineSupplyState state,
        CancellationToken cancellationToken = default);
}
