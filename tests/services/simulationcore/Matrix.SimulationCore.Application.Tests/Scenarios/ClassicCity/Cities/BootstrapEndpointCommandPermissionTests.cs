using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompleteEconomyBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompletePopulationBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailEconomyBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailPopulationBootstrap;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities;

public sealed class BootstrapEndpointCommandPermissionTests
{
    private static readonly Guid CityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid OperationId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public void CompletePopulationEndpointCommand_RequiresClassicCityCreatePermission()
    {
        var command = new CompleteCityPopulationBootstrapEndpointCommand(CityId, OperationId);

        var permissionRequest = Assert.IsAssignableFrom<IRequirePermission>(command);

        Assert.Equal(PermissionKeys.SimulationCoreClassicCityCreate, permissionRequest.PermissionKey);
    }

    [Fact]
    public void FailPopulationEndpointCommand_RequiresClassicCityCreatePermission()
    {
        var command = new FailCityPopulationBootstrapEndpointCommand(CityId, OperationId, "Population.Failed");

        var permissionRequest = Assert.IsAssignableFrom<IRequirePermission>(command);

        Assert.Equal(PermissionKeys.SimulationCoreClassicCityCreate, permissionRequest.PermissionKey);
    }

    [Fact]
    public void CompleteEconomyEndpointCommand_RequiresClassicCityCreatePermission()
    {
        var command = new CompleteCityEconomyBootstrapEndpointCommand(CityId, OperationId);

        var permissionRequest = Assert.IsAssignableFrom<IRequirePermission>(command);

        Assert.Equal(PermissionKeys.SimulationCoreClassicCityCreate, permissionRequest.PermissionKey);
    }

    [Fact]
    public void FailEconomyEndpointCommand_RequiresClassicCityCreatePermission()
    {
        var command = new FailCityEconomyBootstrapEndpointCommand(CityId, OperationId, "Economy.Failed");

        var permissionRequest = Assert.IsAssignableFrom<IRequirePermission>(command);

        Assert.Equal(PermissionKeys.SimulationCoreClassicCityCreate, permissionRequest.PermissionKey);
    }

    [Fact]
    public void InternalBootstrapCommands_RemainUnprotectedForProvisioningOrchestrator()
    {
        object[] commands =
        [
            new CompleteCityPopulationBootstrapCommand(CityId, OperationId),
            new FailCityPopulationBootstrapCommand(CityId, OperationId, "Population.Failed"),
            new CompleteCityEconomyBootstrapCommand(CityId, OperationId),
            new FailCityEconomyBootstrapCommand(CityId, OperationId, "Economy.Failed")
        ];

        foreach (object command in commands)
        {
            Assert.False(command is IRequirePermission);
            Assert.False(command is IRequirePermissions);
        }
    }
}
