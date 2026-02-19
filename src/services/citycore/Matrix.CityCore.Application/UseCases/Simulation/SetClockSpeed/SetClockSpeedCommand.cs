using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.CityCore.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.SetClockSpeed
{
    public sealed record SetClockSpeedCommand(
        Guid SimulationId,
        decimal Multiplier) : IRequest<bool>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.CityCoreSimulationControl;
    }
}
