using Matrix.ArchitectureTesting;
using Matrix.Healthcare.Domain.Patients;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Architecture
{
    public sealed class HealthcareDomainBoundaryTests
    {
        [Fact]
        public void Domain_DoesNotReferenceOtherBoundedContexts()
        {
            BoundedContextDependencyRule.AssertOnlyReferencesMatrixAssemblies(
                assembly: typeof(PatientProfile).Assembly,
                "Matrix.BuildingBlocks.Domain");
        }
    }
}
