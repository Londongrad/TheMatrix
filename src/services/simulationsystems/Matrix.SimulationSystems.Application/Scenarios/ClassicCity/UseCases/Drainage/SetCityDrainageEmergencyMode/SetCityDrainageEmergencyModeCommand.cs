using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.Common;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.SetCityDrainageEmergencyMode
{
    public sealed record SetCityDrainageEmergencyModeCommand(
        Guid CityId,
        bool Enabled)
        : IRequest<CityDrainageStatusDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationSystemsClassicCityManage;
    }
}
