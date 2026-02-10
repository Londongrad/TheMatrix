using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Population.Application.Authorization.Permissions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation
{
    public sealed record InitializeCityPopulationCommand(
        Guid CityId,
        DateOnly CurrentDate,
        int PeopleCount,
        int? RandomSeed,
        CityPopulationEnvironmentInput Environment,
        IReadOnlyCollection<ResidentialBuildingSeedItem> ResidentialBuildings)
        : IRequest<CityPopulationBootstrapSummaryDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.PopulationPeopleInitialize;
    }
}
