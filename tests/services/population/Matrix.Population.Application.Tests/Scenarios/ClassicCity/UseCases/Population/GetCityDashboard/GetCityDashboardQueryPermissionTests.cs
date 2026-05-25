using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.Population.Application.Authorization.Permissions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard
{
    public sealed class GetCityDashboardQueryPermissionTests
    {
        [Fact]
        public void Query_RequiresPopulationPeopleReadPermission()
        {
            var query = new GetCityDashboardQuery(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

            IRequirePermission permissionRequest = Assert.IsAssignableFrom<IRequirePermission>(query);

            Assert.Equal(
                expected: PermissionKeys.PopulationPeopleRead,
                actual: permissionRequest.PermissionKey);
        }
    }
}
