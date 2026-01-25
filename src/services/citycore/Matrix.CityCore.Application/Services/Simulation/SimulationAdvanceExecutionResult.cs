using Matrix.CityCore.Domain.Cities;

namespace Matrix.CityCore.Application.Services.Simulation
{
    public sealed record SimulationAdvanceExecutionResult(
        CityId CityId,
        SimulationAdvanceExecutionStatus Status);
}
