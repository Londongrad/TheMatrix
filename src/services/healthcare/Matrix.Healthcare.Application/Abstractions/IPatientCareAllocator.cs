using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Application.Abstractions;

public interface IPatientCareAllocator
{
    Task<int> AllocateAsync(
        SimulationHostId simulationHostId,
        DateOnly careDate,
        DateTimeOffset assignedAtUtc,
        CancellationToken cancellationToken = default);
}
