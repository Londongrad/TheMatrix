using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.CityCore.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetSimulationKinds
{
    public sealed record GetSimulationKindsQuery
        : IRequest<IReadOnlyList<SimulationKindCatalogItemDto>>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.CityCoreClassicCityCreate;
    }
}
