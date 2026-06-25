using Matrix.ArchitectureTesting;
using Matrix.Healthcare.Infrastructure.Persistence;
using Xunit;

namespace Matrix.Healthcare.Infrastructure.Tests.Architecture
{
    public sealed class HealthcareInfrastructureBoundaryTests
    {
        [Fact]
        public void Infrastructure_ReferencesOnlyHealthcareAndBuildingBlocks()
        {
            BoundedContextDependencyRule.AssertOnlyReferencesMatrixAssemblies(
                assembly: typeof(HealthcareDbContext).Assembly,
                "Matrix.BuildingBlocks.Application",
                "Matrix.BuildingBlocks.Domain",
                "Matrix.BuildingBlocks.Infrastructure",
                "Matrix.Healthcare.Application",
                "Matrix.Healthcare.Domain");
        }
    }
}
