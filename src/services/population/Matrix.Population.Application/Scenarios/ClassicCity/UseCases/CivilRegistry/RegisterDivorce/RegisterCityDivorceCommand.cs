using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Population.Application.Authorization.Permissions;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.CivilRegistry.RegisterDivorce
{
    public sealed record RegisterCityDivorceCommand(
        Guid CityId,
        Guid FirstResidentId,
        Guid SecondResidentId,
        DateOnly CurrentDate) : IRequest<CityCivilRegistryOperationResultDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationCivilRegistryManage;
    }
}
