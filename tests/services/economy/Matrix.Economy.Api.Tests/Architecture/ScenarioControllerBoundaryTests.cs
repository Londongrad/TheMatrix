using Matrix.Economy.Api.Controllers.Scenarios.ClassicCity;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Matrix.Economy.Api.Tests.Architecture
{
    public sealed class ScenarioControllerBoundaryTests
    {
        [Fact]
        public void EconomyControllers_BelongToScenarioBoundary()
        {
            Type[] misplacedControllers = typeof(BudgetController).Assembly
               .GetTypes()
               .Where(type =>
                    !type.IsAbstract &&
                    typeof(ControllerBase).IsAssignableFrom(type) &&
                    (type.Namespace is null ||
                     !type.Namespace.StartsWith(
                         "Matrix.Economy.Api.Controllers.Scenarios.",
                         StringComparison.Ordinal)))
               .ToArray();

            Assert.Empty(misplacedControllers);
        }
    }
}
