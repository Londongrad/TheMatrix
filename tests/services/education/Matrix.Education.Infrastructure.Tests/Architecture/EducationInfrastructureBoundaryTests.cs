using Matrix.ArchitectureTesting;
using Matrix.Education.Infrastructure.Persistence;
using Xunit;

namespace Matrix.Education.Infrastructure.Tests.Architecture
{
    public sealed class EducationInfrastructureBoundaryTests
    {
        [Fact]
        public void Infrastructure_DoesNotReferenceOtherBoundedContexts()
        {
            BoundedContextDependencyRule.AssertOnlyReferencesMatrixAssemblies(
                assembly: typeof(EducationDbContext).Assembly,
                "Matrix.BuildingBlocks.Application",
                "Matrix.BuildingBlocks.Domain",
                "Matrix.BuildingBlocks.Infrastructure",
                "Matrix.Education.Application",
                "Matrix.Education.Contracts",
                "Matrix.Education.Domain",
                "Matrix.Simulation.Primitives");
        }
    }
}
