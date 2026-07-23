using Matrix.ArchitectureTesting;
using Matrix.Education.Integration.Consumers;
using Xunit;

namespace Matrix.Education.Integration.Tests.Architecture
{
    public sealed class EducationIntegrationBoundaryTests
    {
        [Fact]
        public void Integration_ReferencesOnlyEducationApplicationAndExternalContracts()
        {
            BoundedContextDependencyRule.AssertOnlyReferencesMatrixAssemblies(
                assembly: typeof(PopulationResidentFactsConsumer).Assembly,
                "Matrix.Education.Application",
                "Matrix.ScenarioContracts.ClassicCity",
                "Matrix.Population.Contracts",
                "Matrix.SimulationCore.Contracts");
        }
    }
}
