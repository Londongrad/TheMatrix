using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Population.Application.Authorization.Permissions;
using Matrix.Population.Contracts.Models;
using MediatR;

namespace Matrix.Population.Application.UseCases.Population.InitializeCityPopulation
{
    public sealed record InitializeCityPopulationCommand(
        Guid CityId,
        DateOnly CurrentDate,
        int PeopleCount,
        int? RandomSeed,
        IReadOnlyCollection<ResidentialBuildingSeedItem> ResidentialBuildings)
        : IRequest<CityPopulationBootstrapSummaryDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationPeopleInitialize;
    }
}
