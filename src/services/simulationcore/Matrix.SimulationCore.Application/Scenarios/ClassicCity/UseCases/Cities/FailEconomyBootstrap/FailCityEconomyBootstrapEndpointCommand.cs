using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Authorization.Permissions;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailEconomyBootstrap
{
    public sealed record FailCityEconomyBootstrapEndpointCommand(
        Guid CityId,
        Guid OperationId,
        string FailureCode) : IRequest<bool>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationCoreClassicCityCreate;
    }
}
