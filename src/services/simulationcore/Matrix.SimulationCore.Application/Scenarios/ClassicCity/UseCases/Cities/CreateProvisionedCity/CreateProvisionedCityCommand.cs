using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
using MediatR;
using CityProvisioningView =
    Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning.CityProvisioningModel;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateProvisionedCity
{
    public sealed record CreateProvisionedCityCommand(CreateCityCommand City)
        : IRequest<CityProvisioningView>, IRequirePermission
    {
        public string PermissionKey => City.PermissionKey;
    }
}
