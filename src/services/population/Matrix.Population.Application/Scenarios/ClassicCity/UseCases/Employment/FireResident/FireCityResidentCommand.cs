using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Population.Application.Authorization.Permissions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.FireResident
{
    public sealed record FireCityResidentCommand(
        Guid CityId,
        Guid ResidentId,
        DateOnly CurrentDate) : IRequest<CityEmploymentOperationResultDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationEmploymentManage;
    }
}
