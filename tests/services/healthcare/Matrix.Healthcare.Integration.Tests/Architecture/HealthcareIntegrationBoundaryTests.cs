using Matrix.ArchitectureTesting;
using Matrix.Healthcare.Integration.Consumers;
using Xunit;

namespace Matrix.Healthcare.Integration.Tests.Architecture
{
    public sealed class HealthcareIntegrationBoundaryTests
    {
        [Fact]
        public void Integration_ReferencesOnlyHealthcareApplicationAndExternalContracts()
        {
            BoundedContextDependencyRule.AssertOnlyReferencesMatrixAssemblies(
                assembly: typeof(PopulationResidentFactsConsumer).Assembly,
                "Matrix.Healthcare.Application",
                "Matrix.Healthcare.Domain",
                "Matrix.Population.Contracts");
        }
    }
}
