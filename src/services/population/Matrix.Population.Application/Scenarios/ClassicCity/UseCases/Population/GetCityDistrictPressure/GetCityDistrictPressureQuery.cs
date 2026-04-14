using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Population.Application.Authorization.Permissions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDistrictPressure
{
    public sealed record GetCityDistrictPressureQuery(Guid CityId)
        : IRequest<CityPopulationDistrictPressureDto?>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationPeopleRead;
    }
}
