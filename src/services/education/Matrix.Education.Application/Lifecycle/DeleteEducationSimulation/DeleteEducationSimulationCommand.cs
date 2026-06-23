using MediatR;

namespace Matrix.Education.Application.Lifecycle.DeleteEducationSimulation
{
    public sealed record DeleteEducationSimulationCommand(
        Guid SimulationHostId,
        DateTimeOffset DeletedAtUtc)
        : IRequest<DeleteEducationSimulationResult>;
}
