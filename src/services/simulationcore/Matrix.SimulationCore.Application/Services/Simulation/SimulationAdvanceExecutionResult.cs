using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Services.Simulation
{
    public sealed record SimulationAdvanceExecutionResult(
        SimulationId SimulationId,
        SimulationAdvanceExecutionStatus Status,
        int StepsProcessed = 0,
        long RemainingPendingSimulationTicks = 0,
        bool HasRemainingBacklog = false);
}
