using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.Common;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetCity
{
    public sealed record GetCityQuery(Guid CityId) : IRequest<CityDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.CityCoreClassicCityRead;
    }
}
