using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Authorization.Permissions;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Bootstrap.InitializeCityEconomy
{
    public sealed record InitializeCityEconomyCommand(
        Guid CityId,
        string ScenarioKey,
        string? EconomyProfile,
        DateTimeOffset CreatedAtUtc) : IRequest<CityEconomyBootstrapResultDto>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.EconomyBudgetBootstrap;
    }
}
