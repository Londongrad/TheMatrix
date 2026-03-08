using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Population.Application.Authorization.Permissions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.GetEducationCatalog
{
    public sealed record GetCityEducationCatalogQuery(Guid CityId) : IRequest<CityEducationCatalogDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationEducationManage;
    }
}
