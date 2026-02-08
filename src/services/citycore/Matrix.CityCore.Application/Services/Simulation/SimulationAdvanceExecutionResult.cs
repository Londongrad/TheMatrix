using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.Services.Simulation
{
    public sealed record SimulationAdvanceExecutionResult(
        SimulationId SimulationId,
        SimulationAdvanceExecutionStatus Status);
}
