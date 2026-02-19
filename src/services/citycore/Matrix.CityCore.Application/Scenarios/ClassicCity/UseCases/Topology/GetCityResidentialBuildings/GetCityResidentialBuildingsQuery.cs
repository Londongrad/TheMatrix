using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.CityCore.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityResidentialBuildings
{
    public sealed record GetCityResidentialBuildingsQuery(
        Guid CityId,
        Guid? DistrictId) : IRequest<IReadOnlyList<ResidentialBuildingDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.CityCoreClassicCityRead;
    }
}
