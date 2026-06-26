using MediatR;

namespace Matrix.Healthcare.Application.Lifecycle.DeleteHealthcareSimulation
{
    public sealed record DeleteHealthcareSimulationCommand(
        Guid SimulationHostId,
        DateTimeOffset DeletedAtUtc)
        : IRequest<DeleteHealthcareSimulationResult>;
}
