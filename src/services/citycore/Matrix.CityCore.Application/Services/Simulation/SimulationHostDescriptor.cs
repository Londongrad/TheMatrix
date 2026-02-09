using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Services.Simulation
{
    public sealed record SimulationHostDescriptor(
        SimulationId SimulationId,
        SimulationHostId HostId,
        SimulationHostKind HostKind,
        SimulationKind SimulationKind,
        bool IsActive,
        bool IsArchived);
}
