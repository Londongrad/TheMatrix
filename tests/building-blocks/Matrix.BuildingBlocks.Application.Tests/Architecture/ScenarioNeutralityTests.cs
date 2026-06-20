using Matrix.BuildingBlocks.Application.Abstractions;
using Xunit;

namespace Matrix.BuildingBlocks.Application.Tests.Architecture
{
    public sealed class ScenarioNeutralityTests
    {
        [Fact]
        public void ApplicationBuildingBlocks_DoNotContainScenarioTypes()
        {
            string[] scenarioTypes = typeof(IUnitOfWork).Assembly
               .GetTypes()
               .Where(type =>
                    type.Namespace?.Contains(
                        ".Scenarios.",
                        StringComparison.Ordinal) == true)
               .Select(type => type.FullName ?? type.Name)
               .Order(StringComparer.Ordinal)
               .ToArray();

            Assert.Empty(scenarioTypes);
        }
    }
}
