using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.Common;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.
    DispatchCityWaterDistributionMaintenance
{
    public sealed record DispatchCityWaterDistributionMaintenanceCommand(
        Guid CityId,
        string Focus,
        string Intensity,
        bool EmergencyOverride)
        : IRequest<CityWaterDistributionStatusDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationSystemsClassicCityManage;
    }
}
