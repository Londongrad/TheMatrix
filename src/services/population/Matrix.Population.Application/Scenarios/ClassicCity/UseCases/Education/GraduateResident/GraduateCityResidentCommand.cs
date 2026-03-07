using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Population.Application.Authorization.Permissions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Education.GraduateResident
{
    public sealed record GraduateCityResidentCommand(
        Guid CityId,
        Guid ResidentId,
        string TargetEducationLevel,
        DateOnly CurrentDate) : IRequest<CityEducationOperationResultDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationEducationManage;
    }
}
