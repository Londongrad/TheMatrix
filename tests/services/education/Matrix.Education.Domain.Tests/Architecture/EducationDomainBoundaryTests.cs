using Matrix.ArchitectureTesting;
using Matrix.Education.Domain.Students;
using Xunit;

namespace Matrix.Education.Domain.Tests.Architecture
{
    public sealed class EducationDomainBoundaryTests
    {
        [Fact]
        public void Domain_DoesNotReferenceOtherBoundedContexts()
        {
            BoundedContextDependencyRule.AssertOnlyReferencesMatrixAssemblies(
                assembly: typeof(StudentProfile).Assembly,
                "Matrix.BuildingBlocks.Domain");
        }
    }
}
