using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Economy.Application.Authorization.Permissions;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy
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
