using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Population.Application.Authorization.Permissions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.WithdrawResident
{
    public sealed record WithdrawCityResidentFromStudyCommand(
        Guid CityId,
        Guid ResidentId,
        DateOnly CurrentDate) : IRequest<CityEducationOperationResultDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationEducationManage;
    }
}
