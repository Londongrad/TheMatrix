using MediatR;

namespace Matrix.Education.Application.Lifecycle.RegisterEducationSimulation;

public sealed record RegisterEducationSimulationCommand(Guid SimulationHostId, string ScenarioKey, string HostTypeKey) : IRequest<bool>;
