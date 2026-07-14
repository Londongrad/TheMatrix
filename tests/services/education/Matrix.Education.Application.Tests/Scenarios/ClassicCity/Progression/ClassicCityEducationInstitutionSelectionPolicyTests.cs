using Matrix.Education.Application.Scenarios.ClassicCity.Progression;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Xunit;

namespace Matrix.Education.Application.Tests.Scenarios.ClassicCity.Progression
{
    public sealed class ClassicCityEducationInstitutionSelectionPolicyTests
    {
        private static readonly SimulationHostId HostId = new(Guid.NewGuid());
        private readonly ClassicCityEducationInstitutionSelectionPolicy _policy = new();

        [Fact]
        public void TryReserveInstitution_CompulsoryStage_UsesSchoolCapacity()
        {
            EducationInstitution school = CreateInstitution("School", capacity: 2);

            EducationInstitution? selected = _policy.TryReserveInstitution(
                new ResidentId(Guid.NewGuid()),
                ClassicCityEducationStageCatalog.Primary,
                [school]);

            Assert.Same(school, selected);
            Assert.Equal(1, school.CurrentEnrollmentCount);
        }

        [Fact]
        public void TryReserveInstitution_HigherStage_SkipsSchoolAndUsesUniversity()
        {
            EducationInstitution school = CreateInstitution("School", capacity: 2);
            EducationInstitution university = CreateInstitution("University", capacity: 2);

            EducationInstitution? selected = _policy.TryReserveInstitution(
                new ResidentId(Guid.NewGuid()),
                ClassicCityEducationStageCatalog.Higher,
                [school, university]);

            Assert.Same(university, selected);
            Assert.Equal(0, school.CurrentEnrollmentCount);
            Assert.Equal(1, university.CurrentEnrollmentCount);
        }

        [Fact]
        public void TryReserveInstitution_WhenEligibleInstitutionsAreFull_ReturnsNull()
        {
            EducationInstitution school = CreateInstitution("School", capacity: 1);
            Assert.True(school.TryReserveSeats(1));

            EducationInstitution? selected = _policy.TryReserveInstitution(
                new ResidentId(Guid.NewGuid()),
                ClassicCityEducationStageCatalog.Primary,
                [school]);

            Assert.Null(selected);
            Assert.Equal(1, school.CurrentEnrollmentCount);
        }

        private static EducationInstitution CreateInstitution(string kind, int capacity)
        {
            return EducationInstitution.Create(
                id: EducationInstitutionId.New(),
                simulationHostId: HostId,
                name: $"{kind} institution",
                kind: new EducationInstitutionKindKey(kind),
                capacity: capacity);
        }
    }
}
