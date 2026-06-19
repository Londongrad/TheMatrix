using Matrix.Population.Application.Abstractions;
using Xunit;

namespace Matrix.Population.Application.Tests.Architecture
{
    public sealed class ClassicCityApplicationBoundaryTests
    {
        [Fact]
        public void CityApplicationTypes_BelongToClassicCityScenario()
        {
            Type[] misplacedTypes = typeof(IPersonLifecycleExtension).Assembly
               .GetTypes()
               .Where(type =>
                    type.Name.Contains("City", StringComparison.Ordinal) &&
                    (type.Namespace is null ||
                     !type.Namespace.StartsWith(
                         "Matrix.Population.Application.Scenarios.ClassicCity",
                         StringComparison.Ordinal)))
               .ToArray();

            Assert.Empty(misplacedTypes);
        }
    }
}
