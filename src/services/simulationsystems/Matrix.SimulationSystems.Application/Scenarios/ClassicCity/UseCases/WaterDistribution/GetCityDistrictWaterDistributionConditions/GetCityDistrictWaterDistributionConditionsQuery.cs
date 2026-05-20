using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.GetCityDistrictWaterDistributionConditions
{
    public sealed record GetCityDistrictWaterDistributionConditionsQuery(Guid CityId)
        : IRequest<CityDistrictWaterDistributionConditionsDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationSystemsClassicCityRead;
    }
}
