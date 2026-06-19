using Matrix.Population.Domain.Entities;
using Xunit;

namespace Matrix.Population.Domain.Tests.Architecture
{
    public sealed class ClassicCityDomainBoundaryTests
    {
        [Fact]
        public void CityDomainTypes_BelongToClassicCityScenario()
        {
            Type[] misplacedTypes = typeof(Person).Assembly
               .GetTypes()
               .Where(type =>
                    type.Name.Contains("City", StringComparison.Ordinal) &&
                    (type.Namespace is null ||
                     !type.Namespace.StartsWith(
                         "Matrix.Population.Domain.Scenarios.ClassicCity",
                         StringComparison.Ordinal)))
               .ToArray();

            Assert.Empty(misplacedTypes);
        }
    }
}
