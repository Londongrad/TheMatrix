using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.GetCityDistrictHeatingConditions
{
    public sealed record GetCityDistrictHeatingConditionsQuery(Guid CityId)
        : IRequest<CityDistrictHeatingConditionsDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationSystemsClassicCityRead;
    }
}
