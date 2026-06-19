using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Population.Application.Scenarios.ClassicCity.Authorization.Permissions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Employment.RetireResident
{
    public sealed record RetireCityResidentCommand(
        Guid CityId,
        Guid ResidentId,
        DateOnly CurrentDate) : IRequest<CityEmploymentOperationResultDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationEmploymentManage;
    }
}
