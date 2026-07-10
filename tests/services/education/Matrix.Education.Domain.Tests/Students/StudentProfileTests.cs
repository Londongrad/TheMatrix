using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Matrix.Education.Domain.Programs;
using Xunit;

namespace Matrix.Education.Domain.Tests.Students
{
    public sealed class StudentProfileTests
    {
        private static readonly DateTimeOffset SynchronizedAtUtc = new(
            year: 2026,
            month: 6,
            day: 21,
            hour: 10,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        [Fact]
        public void Register_CreatesActivePopulationReference()
        {
            ResidentId residentId = new(Guid.NewGuid());
            SimulationHostId hostId = new(Guid.NewGuid());
            var birthDate = new DateOnly(2010, 5, 12);

            StudentProfile profile = StudentProfile.Register(
                residentId: residentId,
                simulationHostId: hostId,
                birthDate: birthDate,
                isAlive: true,
                isActive: true,
                sourceRevision: 10,
                synchronizedAtUtc: SynchronizedAtUtc);

            Assert.Equal(residentId, profile.ResidentId);
            Assert.Equal(hostId, profile.SimulationHostId);
            Assert.Equal(birthDate, profile.BirthDate);
            Assert.True(profile.IsAlive);
            Assert.True(profile.IsActive);
            Assert.Equal(10, profile.LastSourceRevision);
            Assert.Equal(SynchronizedAtUtc, profile.LastSynchronizedAtUtc);
        }

        [Fact]
        public void SynchronizeResidentFacts_UpdatesReplicatedFacts()
        {
            StudentProfile profile = CreateProfile();
            var nextBirthDate = new DateOnly(2011, 3, 2);

            bool accepted = profile.TrySynchronizeResidentFacts(
                simulationHostId: profile.SimulationHostId,
                birthDate: nextBirthDate,
                isAlive: false,
                isActive: false,
                sourceRevision: 11,
                synchronizedAtUtc: SynchronizedAtUtc.AddMinutes(1),
                lifecycleRevision: 1);

            Assert.True(accepted);
            Assert.Equal(nextBirthDate, profile.BirthDate);
            Assert.False(profile.IsAlive);
            Assert.False(profile.IsActive);
            Assert.Equal(1, profile.LastLifecycleRevision);
        }

        [Fact]
        public void SynchronizeResidentFacts_AcceptsNewLifecycleWithStaleSourceRevision()
        {
            StudentProfile profile = CreateProfile();

            bool accepted = profile.TrySynchronizeResidentFacts(
                simulationHostId: profile.SimulationHostId,
                birthDate: new DateOnly(2000, 1, 1),
                isAlive: false,
                isActive: false,
                sourceRevision: 10,
                synchronizedAtUtc: SynchronizedAtUtc.AddMinutes(1),
                lifecycleRevision: 1);

            Assert.True(accepted);
            Assert.False(profile.IsAlive);
            Assert.True(profile.IsActive);
            Assert.Equal(new DateOnly(2010, 5, 12), profile.BirthDate);
            Assert.Equal(10, profile.LastSourceRevision);
            Assert.Equal(1, profile.LastLifecycleRevision);
        }

        [Fact]
        public void SynchronizeResidentFacts_IgnoresDuplicateAndOlderRevisions()
        {
            StudentProfile profile = CreateProfile();

            bool duplicateAccepted = profile.TrySynchronizeResidentFacts(
                simulationHostId: profile.SimulationHostId,
                birthDate: new DateOnly(2000, 1, 1),
                isAlive: false,
                isActive: false,
                sourceRevision: 10,
                synchronizedAtUtc: SynchronizedAtUtc.AddMinutes(1));
            bool olderAccepted = profile.TrySynchronizeResidentFacts(
                simulationHostId: profile.SimulationHostId,
                birthDate: new DateOnly(2000, 1, 1),
                isAlive: false,
                isActive: false,
                sourceRevision: 9,
                synchronizedAtUtc: SynchronizedAtUtc.AddMinutes(2));

            Assert.False(duplicateAccepted);
            Assert.False(olderAccepted);
            Assert.Equal(new DateOnly(2010, 5, 12), profile.BirthDate);
            Assert.True(profile.IsAlive);
            Assert.Equal(10, profile.LastSourceRevision);
        }

        [Fact]
        public void SynchronizeResidentFacts_RejectsMovingProfileBetweenHosts()
        {
            StudentProfile profile = CreateProfile();

            Assert.Throws<InvalidOperationException>(() => profile.TrySynchronizeResidentFacts(
                simulationHostId: new SimulationHostId(Guid.NewGuid()),
                birthDate: profile.BirthDate,
                isAlive: profile.IsAlive,
                isActive: profile.IsActive,
                sourceRevision: 11,
                synchronizedAtUtc: SynchronizedAtUtc.AddMinutes(1)));

            Assert.Equal(10, profile.LastSourceRevision);
        }

        [Fact]
        public void Register_CanCreateInactivePopulationReference()
        {
            StudentProfile profile = StudentProfile.Register(
                residentId: new ResidentId(Guid.NewGuid()),
                simulationHostId: new SimulationHostId(Guid.NewGuid()),
                birthDate: new DateOnly(2010, 1, 1),
                isAlive: false,
                isActive: false,
                sourceRevision: 1,
                synchronizedAtUtc: SynchronizedAtUtc);

            Assert.False(profile.IsAlive);
            Assert.False(profile.IsActive);
        }

        [Fact]
        public void DeactivateAndReactivate_KeepLifecycleIdempotent()
        {
            StudentProfile profile = CreateProfile();

            bool deactivated = profile.TryDeactivate(
                sourceRevision: 11,
                synchronizedAtUtc: SynchronizedAtUtc.AddMinutes(1));
            bool reactivated = profile.TryReactivate(
                sourceRevision: 12,
                synchronizedAtUtc: SynchronizedAtUtc.AddMinutes(2));

            Assert.True(deactivated);
            Assert.True(reactivated);
            Assert.True(profile.IsActive);
            Assert.Equal(SynchronizedAtUtc.AddMinutes(2), profile.LastSynchronizedAtUtc);
        }

        [Fact]
        public void RecordStageCompletion_AvailableStudent_StoresEducationAttainment()
        {
            StudentProfile profile = CreateProfile();
            var stage = new EducationStageKey("upper-secondary");
            var completedOn = new DateOnly(2028, 6, 30);

            profile.RecordStageCompletion(stage, completedOn);

            Assert.Equal(stage, profile.CompletedStage);
            Assert.Equal(completedOn, profile.CompletedStageOn);
        }

        [Fact]
        public void RecordStageCompletion_UnavailableStudent_RejectsAttainment()
        {
            StudentProfile profile = CreateProfile();
            profile.TryDeactivate(
                sourceRevision: 11,
                synchronizedAtUtc: SynchronizedAtUtc.AddMinutes(1));

            Assert.Throws<InvalidOperationException>(() => profile.RecordStageCompletion(
                new EducationStageKey("upper-secondary"),
                new DateOnly(2028, 6, 30)));
        }

        [Fact]
        public void Register_RejectsNonUtcSynchronizationTimestamp()
        {
            Assert.Throws<ArgumentException>(() => StudentProfile.Register(
                residentId: new ResidentId(Guid.NewGuid()),
                simulationHostId: new SimulationHostId(Guid.NewGuid()),
                birthDate: new DateOnly(2010, 1, 1),
                isAlive: true,
                isActive: true,
                sourceRevision: 1,
                synchronizedAtUtc: SynchronizedAtUtc.ToOffset(TimeSpan.FromHours(3))));
        }

        private static StudentProfile CreateProfile()
        {
            return StudentProfile.Register(
                residentId: new ResidentId(Guid.NewGuid()),
                simulationHostId: new SimulationHostId(Guid.NewGuid()),
                birthDate: new DateOnly(2010, 5, 12),
                isAlive: true,
                isActive: true,
                sourceRevision: 10,
                synchronizedAtUtc: SynchronizedAtUtc);
        }
    }
}
