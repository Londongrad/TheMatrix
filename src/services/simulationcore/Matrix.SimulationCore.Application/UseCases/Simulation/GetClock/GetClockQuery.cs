using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.SimulationCore.Application.UseCases.Simulation.GetClock
{
    public sealed record GetClockQuery(Guid SimulationId) : IRequest<ClockDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationCoreSimulationRead;
    }
}
