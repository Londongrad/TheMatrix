using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Population.Application.Authorization.Permissions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityPopulationSummary
{
    public sealed record GetCityPopulationSummaryQuery(
        Guid CityId,
        DateOnly CurrentDate) : IRequest<CityPopulationSummaryDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationPeopleRead;
    }
}
