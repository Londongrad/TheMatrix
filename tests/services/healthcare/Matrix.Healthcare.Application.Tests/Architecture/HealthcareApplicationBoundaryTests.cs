using Matrix.ArchitectureTesting;
using Matrix.Healthcare.Application.Patients.SynchronizePatientProfiles;
using Xunit;

namespace Matrix.Healthcare.Application.Tests.Architecture
{
    public sealed class HealthcareApplicationBoundaryTests
    {
        [Fact]
        public void Application_DoesNotReferenceOtherBoundedContexts()
        {
            BoundedContextDependencyRule.AssertOnlyReferencesMatrixAssemblies(
                assembly: typeof(SynchronizePatientProfilesCommand).Assembly,
                "Matrix.BuildingBlocks.Application",
                "Matrix.Healthcare.Domain");
        }
    }
}
