using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Population.Application.Authorization.Permissions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.HireResident
{
    public sealed record HireCityResidentCommand(
        Guid CityId,
        Guid ResidentId,
        string JobTitle,
        DateOnly CurrentDate) : IRequest<CityEmploymentOperationResultDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationEmploymentManage;
    }
}
