using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Authorization.Permissions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompleteEconomyBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompletePopulationBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailEconomyBootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailPopulationBootstrap;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.Cities
{
    public sealed class BootstrapEndpointCommandPermissionTests
    {
        private static readonly Guid CityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        private static readonly Guid OperationId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        [Fact]
        public void CompletePopulationEndpointCommand_RequiresClassicCityCreatePermission()
        {
            var command = new CompleteCityPopulationBootstrapEndpointCommand(
                CityId: CityId,
                OperationId: OperationId);

            IRequirePermission permissionRequest = Assert.IsAssignableFrom<IRequirePermission>(command);

            Assert.Equal(
                expected: PermissionKeys.SimulationCoreClassicCityCreate,
                actual: permissionRequest.PermissionKey);
        }

        [Fact]
        public void FailPopulationEndpointCommand_RequiresClassicCityCreatePermission()
        {
            var command = new FailCityPopulationBootstrapEndpointCommand(
                CityId: CityId,
                OperationId: OperationId,
                FailureCode: "Population.Failed");

            IRequirePermission permissionRequest = Assert.IsAssignableFrom<IRequirePermission>(command);

            Assert.Equal(
                expected: PermissionKeys.SimulationCoreClassicCityCreate,
                actual: permissionRequest.PermissionKey);
        }

        [Fact]
        public void CompleteEconomyEndpointCommand_RequiresClassicCityCreatePermission()
        {
            var command = new CompleteCityEconomyBootstrapEndpointCommand(
                CityId: CityId,
                OperationId: OperationId);

            IRequirePermission permissionRequest = Assert.IsAssignableFrom<IRequirePermission>(command);

            Assert.Equal(
                expected: PermissionKeys.SimulationCoreClassicCityCreate,
                actual: permissionRequest.PermissionKey);
        }

        [Fact]
        public void FailEconomyEndpointCommand_RequiresClassicCityCreatePermission()
        {
            var command = new FailCityEconomyBootstrapEndpointCommand(
                CityId: CityId,
                OperationId: OperationId,
                FailureCode: "Economy.Failed");

            IRequirePermission permissionRequest = Assert.IsAssignableFrom<IRequirePermission>(command);

            Assert.Equal(
                expected: PermissionKeys.SimulationCoreClassicCityCreate,
                actual: permissionRequest.PermissionKey);
        }

        [Fact]
        public void InternalBootstrapCommands_RemainUnprotectedForProvisioningOrchestrator()
        {
            object[] commands =
            [
                new CompleteCityPopulationBootstrapCommand(
                    CityId: CityId,
                    OperationId: OperationId),
                new FailCityPopulationBootstrapCommand(
                    CityId: CityId,
                    OperationId: OperationId,
                    FailureCode: "Population.Failed"),
                new CompleteCityEconomyBootstrapCommand(
                    CityId: CityId,
                    OperationId: OperationId),
                new FailCityEconomyBootstrapCommand(
                    CityId: CityId,
                    OperationId: OperationId,
                    FailureCode: "Economy.Failed")
            ];

            foreach (object command in commands)
            {
                Assert.False(command is IRequirePermission);
                Assert.False(command is IRequirePermissions);
            }
        }
    }
}
