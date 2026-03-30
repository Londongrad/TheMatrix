using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateProvisionedCity
{
    public sealed record CreateProvisionedCityCommand(CreateCityCommand City)
        : IRequest<CityProvisioningView>, IRequirePermission
    {
        public string PermissionKey => City.PermissionKey;
    }
}
