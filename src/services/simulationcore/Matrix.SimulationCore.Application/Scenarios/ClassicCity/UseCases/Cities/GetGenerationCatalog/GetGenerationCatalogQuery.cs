using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Authorization.Permissions;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetGenerationCatalog
{
    public sealed record GetGenerationCatalogQuery : IRequest<CityGenerationCatalogDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationCoreClassicCityCreate;
    }
}
