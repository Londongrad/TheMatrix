using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Population.Application.Authorization.Permissions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard
{
    public sealed record GetCityDashboardQuery(Guid CityId)
        : IRequest<CityPopulationDashboardDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationPeopleRead;
    }
}
