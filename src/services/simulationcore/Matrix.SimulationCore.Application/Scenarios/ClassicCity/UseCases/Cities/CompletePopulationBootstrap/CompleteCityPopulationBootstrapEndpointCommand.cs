using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Authorization.Permissions;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompletePopulationBootstrap
{
    public sealed record CompleteCityPopulationBootstrapEndpointCommand(
        Guid CityId,
        Guid OperationId) : IRequest<bool>, IRequirePermission
    {
        public string PermissionKey => PermissionKeys.SimulationCoreClassicCityCreate;
    }
}
