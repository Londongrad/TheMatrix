using Matrix.SimulationCore.Contracts.Scenarios.Catalog;
using Xunit;

namespace Matrix.SimulationCore.Api.Tests.Controllers
{
    public sealed class SimulationScenarioApiRoutesTests
    {
        [Fact]
        public void CatalogRoute_HasStablePublicPath()
        {
            Assert.Equal("api/scenarios", SimulationScenarioApiRoutes.CatalogRoute);
            Assert.Equal("/api/scenarios", SimulationScenarioApiRoutes.CatalogPath);
        }
    }
}
