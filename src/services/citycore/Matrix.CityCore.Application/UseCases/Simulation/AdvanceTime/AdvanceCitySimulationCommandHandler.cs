using Matrix.CityCore.Application.Services.Simulation;
using Matrix.CityCore.Application.Services.Simulation.Abstractions;
using Matrix.CityCore.Domain.Cities;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.AdvanceTime
{
    public sealed class AdvanceCitySimulationCommandHandler(ISimulationAdvanceExecutor executor)
        : IRequestHandler<AdvanceCitySimulationCommand, bool>
    {
        public async Task<bool> Handle(
            AdvanceCitySimulationCommand request,
            CancellationToken cancellationToken)
        {
            SimulationAdvanceExecutionResult result = await executor.ExecuteAsync(
                cityId: new CityId(request.CityId),
                realDelta: request.RealDelta,
                cancellationToken: cancellationToken);

            return result.Status != SimulationAdvanceExecutionStatus.NotFound;
        }
    }
}
