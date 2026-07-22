using System.Data;
using Matrix.Education.Application.Abstractions;
using Matrix.Education.Domain.Simulation;
using Matrix.Simulation.Primitives;
using MediatR;

namespace Matrix.Education.Application.Lifecycle.RegisterEducationSimulation;

public sealed class RegisterEducationSimulationCommandHandler(
    IEducationSimulationRuntimeRepository runtimeRepository,
    IEducationSimulationDeletionRepository deletionRepository,
    IEducationUnitOfWork unitOfWork) : IRequestHandler<RegisterEducationSimulationCommand, bool>
{
    public Task<bool> Handle(RegisterEducationSimulationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var hostId = new SimulationHostId(request.SimulationHostId);
        var runtime = new SimulationRuntimeKey(new SimulationScenarioKey(request.ScenarioKey), new SimulationHostTypeKey(request.HostTypeKey));
        return unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (await deletionRepository.GetDeletedAtUtcAsync(hostId, token) is not null)
                return false;
            await runtimeRepository.EnsureAsync(hostId, runtime, token);
            await unitOfWork.SaveChangesAsync(token);
            return true;
        }, cancellationToken, IsolationLevel.Serializable);
    }
}
