using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Patients
{
    public sealed class PatientProfileTests
    {
        private static readonly PatientId PatientId =
            new(Guid.Parse("11111111-2222-3333-4444-555555555555"));

        private static readonly SimulationHostId SimulationHostId =
            new(Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa"));

        [Fact]
        public void Register_CreatesHealthcareOwnedPatientReference()
        {
            PatientProfile profile = CreateProfile();

            Assert.Equal(PatientId, profile.PatientId);
            Assert.Equal(SimulationHostId, profile.SimulationHostId);
            Assert.Equal(new DateOnly(2027, 4, 3), profile.BirthDate);
            Assert.Equal(PatientSex.Female, profile.Sex);
            Assert.True(profile.IsEligibleForCare);
            Assert.Equal(7, profile.LastSourceRevision);
        }

        [Fact]
        public void TrySynchronizeResidentFacts_WhenRevisionAdvances_UpdatesDemographics()
        {
            PatientProfile profile = CreateProfile();
            DateTimeOffset synchronizedAtUtc =
                DateTimeOffset.Parse("2048-05-07T10:00:00+00:00");

            bool changed = profile.TrySynchronizeResidentFacts(
                simulationHostId: SimulationHostId,
                birthDate: new DateOnly(2027, 4, 4),
                sex: PatientSex.Male,
                isAlive: false,
                isActive: false,
                sourceRevision: 8,
                synchronizedAtUtc: synchronizedAtUtc,
                lifecycleRevision: 1);

            Assert.True(changed);
            Assert.Equal(new DateOnly(2027, 4, 4), profile.BirthDate);
            Assert.Equal(PatientSex.Male, profile.Sex);
            Assert.False(profile.IsEligibleForCare);
            Assert.Equal(8, profile.LastSourceRevision);
            Assert.Equal(1, profile.LastLifecycleRevision);
            Assert.Equal(synchronizedAtUtc, profile.LastSynchronizedAtUtc);
        }

        [Fact]
        public void TrySynchronizeResidentFacts_WhenOnlyLifecycleAdvances_UpdatesAliveState()
        {
            PatientProfile profile = CreateProfile();

            bool changed = profile.TrySynchronizeResidentFacts(
                simulationHostId: SimulationHostId,
                birthDate: new DateOnly(2030, 1, 1),
                sex: PatientSex.Male,
                isAlive: false,
                isActive: false,
                sourceRevision: 7,
                synchronizedAtUtc: DateTimeOffset.Parse("2048-05-08T10:00:00+00:00"),
                lifecycleRevision: 1);

            Assert.True(changed);
            Assert.False(profile.IsAlive);
            Assert.True(profile.IsActive);
            Assert.Equal(new DateOnly(2027, 4, 3), profile.BirthDate);
            Assert.Equal(PatientSex.Female, profile.Sex);
            Assert.Equal(7, profile.LastSourceRevision);
            Assert.Equal(1, profile.LastLifecycleRevision);
        }

        [Fact]
        public void TrySynchronizeResidentFacts_WhenRevisionIsStale_KeepsCurrentFacts()
        {
            PatientProfile profile = CreateProfile();

            bool changed = profile.TrySynchronizeResidentFacts(
                simulationHostId: SimulationHostId,
                birthDate: new DateOnly(2030, 1, 1),
                sex: PatientSex.Male,
                isAlive: false,
                isActive: false,
                sourceRevision: 7,
                synchronizedAtUtc: DateTimeOffset.Parse("2048-05-08T10:00:00+00:00"));

            Assert.False(changed);
            Assert.Equal(new DateOnly(2027, 4, 3), profile.BirthDate);
            Assert.Equal(PatientSex.Female, profile.Sex);
            Assert.True(profile.IsEligibleForCare);
        }

        [Fact]
        public void TrySynchronizeResidentFacts_WhenHostChanges_ThrowsInvalidOperationException()
        {
            PatientProfile profile = CreateProfile();

            Assert.Throws<InvalidOperationException>(() =>
                profile.TrySynchronizeResidentFacts(
                    simulationHostId: new SimulationHostId(Guid.NewGuid()),
                    birthDate: profile.BirthDate,
                    sex: profile.Sex,
                    isAlive: profile.IsAlive,
                    isActive: profile.IsActive,
                    sourceRevision: 8,
                    synchronizedAtUtc: DateTimeOffset.Parse("2048-05-07T10:00:00+00:00")));
        }

        [Fact]
        public void Register_WhenTimestampIsNotUtc_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                PatientProfile.Register(
                    patientId: PatientId,
                    simulationHostId: SimulationHostId,
                    birthDate: new DateOnly(2027, 4, 3),
                    sex: PatientSex.Female,
                    isAlive: true,
                    isActive: true,
                    sourceRevision: 7,
                    synchronizedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+03:00")));
        }

        private static PatientProfile CreateProfile()
        {
            return PatientProfile.Register(
                patientId: PatientId,
                simulationHostId: SimulationHostId,
                birthDate: new DateOnly(2027, 4, 3),
                sex: PatientSex.Female,
                isAlive: true,
                isActive: true,
                sourceRevision: 7,
                synchronizedAtUtc: DateTimeOffset.Parse("2048-05-06T10:00:00+00:00"));
        }
    }
}
