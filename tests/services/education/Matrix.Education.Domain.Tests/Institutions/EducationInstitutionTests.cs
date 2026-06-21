using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Simulation;
using Xunit;

namespace Matrix.Education.Domain.Tests.Institutions
{
    public sealed class EducationInstitutionTests
    {
        [Fact]
        public void Create_InitializesActiveInstitutionWithEmptyCapacityUsage()
        {
            EducationInstitution institution = CreateInstitution(capacity: 120);

            Assert.Equal("Central University", institution.Name);
            Assert.Equal("university", institution.Kind.Value);
            Assert.Equal(120, institution.Capacity);
            Assert.Equal(0, institution.CurrentEnrollmentCount);
            Assert.Equal(120, institution.AvailableSeatCount);
            Assert.True(institution.IsActive);
        }

        [Fact]
        public void TryReserveSeats_AppliesGroupedReservationOnce()
        {
            EducationInstitution institution = CreateInstitution(capacity: 120);

            bool reserved = institution.TryReserveSeats(80);
            bool overflowReserved = institution.TryReserveSeats(41);

            Assert.True(reserved);
            Assert.False(overflowReserved);
            Assert.Equal(80, institution.CurrentEnrollmentCount);
            Assert.Equal(40, institution.AvailableSeatCount);
        }

        [Fact]
        public void TryReserveSeats_WhenInactive_DoesNotMutateCapacityUsage()
        {
            EducationInstitution institution = CreateInstitution(capacity: 20);
            institution.Deactivate();

            bool reserved = institution.TryReserveSeats(1);

            Assert.False(reserved);
            Assert.Equal(0, institution.CurrentEnrollmentCount);
        }

        [Fact]
        public void ReleaseSeats_ReleasesGroupedReservation()
        {
            EducationInstitution institution = CreateInstitution(capacity: 20);
            institution.TryReserveSeats(12);

            institution.ReleaseSeats(7);

            Assert.Equal(5, institution.CurrentEnrollmentCount);
        }

        [Fact]
        public void ChangeCapacity_RejectsValueBelowCurrentEnrollment()
        {
            EducationInstitution institution = CreateInstitution(capacity: 20);
            institution.TryReserveSeats(12);

            Assert.Throws<InvalidOperationException>(() => institution.ChangeCapacity(11));
            Assert.Equal(20, institution.Capacity);
        }

        [Fact]
        public void Create_RejectsInvalidCapacity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateInstitution(capacity: 0));
        }

        private static EducationInstitution CreateInstitution(int capacity)
        {
            return EducationInstitution.Create(
                id: EducationInstitutionId.New(),
                simulationHostId: new SimulationHostId(Guid.NewGuid()),
                name: "Central University",
                kind: new EducationInstitutionKindKey("university"),
                capacity: capacity,
                locationAnchorId: new LocationAnchorId(Guid.NewGuid()));
        }
    }
}
