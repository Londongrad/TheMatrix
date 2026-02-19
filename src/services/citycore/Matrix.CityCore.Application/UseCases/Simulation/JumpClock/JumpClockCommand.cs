using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.CityCore.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.JumpClock
{
    public sealed record JumpClockCommand(
        Guid SimulationId,
        DateTimeOffset NewSimTimeUtc) : IRequest<bool>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.CityCoreSimulationControl;
    }
}
