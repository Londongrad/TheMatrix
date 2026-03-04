using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Population.Application.Authorization.Permissions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentDetails
{
    public sealed record GetCityResidentDetailsQuery(
        Guid CityId,
        Guid PersonId,
        DateOnly CurrentDate) : IRequest<CityResidentDetailsDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationPeopleRead;
    }
}
