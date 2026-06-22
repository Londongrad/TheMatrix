using Matrix.ArchitectureTesting;
using Matrix.Education.Application.Progression.AdvanceEducationProgression;
using Xunit;

namespace Matrix.Education.Application.Tests.Architecture
{
    public sealed class EducationApplicationBoundaryTests
    {
        [Fact]
        public void Application_DoesNotReferenceOtherBoundedContexts()
        {
            BoundedContextDependencyRule.AssertOnlyReferencesMatrixAssemblies(
                assembly: typeof(AdvanceEducationProgressionCommand).Assembly,
                "Matrix.BuildingBlocks.Application",
                "Matrix.Education.Domain");
        }
    }
}
