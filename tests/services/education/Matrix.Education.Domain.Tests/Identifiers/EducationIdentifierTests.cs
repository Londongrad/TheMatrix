using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Xunit;

namespace Matrix.Education.Domain.Tests.Identifiers
{
    public sealed class EducationIdentifierTests
    {
        [Fact]
        public void Identifiers_PreserveNonEmptyValues()
        {
            var value = Guid.NewGuid();

            Assert.Equal(value, new SimulationHostId(value).Value);
            Assert.Equal(value, new ResidentId(value).Value);
            Assert.Equal(value, new EducationInstitutionId(value).Value);
            Assert.Equal(value, new LocationAnchorId(value).Value);
            Assert.Equal(value, new EnrollmentId(value).Value);
        }

        [Fact]
        public void Identifiers_RejectEmptyValues()
        {
            Assert.Throws<DomainException>(() => new SimulationHostId(Guid.Empty));
            Assert.Throws<DomainException>(() => new ResidentId(Guid.Empty));
            Assert.Throws<DomainException>(() => new EducationInstitutionId(Guid.Empty));
            Assert.Throws<DomainException>(() => new LocationAnchorId(Guid.Empty));
            Assert.Throws<DomainException>(() => new EnrollmentId(Guid.Empty));
        }

        [Fact]
        public void GeneratedIdentifiers_AreNonEmpty()
        {
            Assert.NotEqual(Guid.Empty, EducationInstitutionId.New().Value);
            Assert.NotEqual(Guid.Empty, EnrollmentId.New().Value);
        }
    }
}
