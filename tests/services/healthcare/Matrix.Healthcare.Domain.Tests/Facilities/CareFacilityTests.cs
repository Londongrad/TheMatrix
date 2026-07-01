using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Facilities
{
    public sealed class CareFacilityTests
    {
        private static readonly CareFacilityId FacilityId = new(Guid.NewGuid());
        private static readonly SimulationHostId HostId = new(Guid.NewGuid());
        private static readonly DateTimeOffset SynchronizedAtUtc =
            DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");

        [Fact]
        public void Register_PreservesScenarioNeutralProvisioningFacts()
        {
            var locationAnchorId = new LocationAnchorId(Guid.NewGuid());

            CareFacility facility = CareFacility.Register(
                id: FacilityId,
                simulationHostId: HostId,
                name: "Central Hospital",
                kind: new CareFacilityKindKey("Hospital"),
                locationAnchorId: locationAnchorId,
                dailyPatientCapacity: 240,
                isActive: true,
                sourceRevision: 7,
                synchronizedAtUtc: SynchronizedAtUtc);

            Assert.Equal(FacilityId, facility.CareFacilityId);
            Assert.Equal(HostId, facility.SimulationHostId);
            Assert.Equal("Central Hospital", facility.Name);
            Assert.Equal("Hospital", facility.Kind.Value);
            Assert.Equal(locationAnchorId, facility.LocationAnchorId);
            Assert.Equal(240, facility.DailyPatientCapacity);
            Assert.True(facility.IsActive);
            Assert.Equal(7, facility.LastSourceRevision);
        }

        [Fact]
        public void SynchronizeProvisioning_WhenRevisionAdvances_UpdatesCatalogFacts()
        {
            CareFacility facility = CreateFacility();
            var nextAnchorId = new LocationAnchorId(Guid.NewGuid());

            bool changed = facility.TrySynchronizeProvisioning(
                simulationHostId: HostId,
                name: "Regional Clinic",
                kind: new CareFacilityKindKey("PrimaryCare"),
                locationAnchorId: nextAnchorId,
                dailyPatientCapacity: 80,
                isActive: false,
                sourceRevision: 8,
                synchronizedAtUtc: SynchronizedAtUtc.AddMinutes(1));

            Assert.True(changed);
            Assert.Equal("Regional Clinic", facility.Name);
            Assert.Equal("PrimaryCare", facility.Kind.Value);
            Assert.Equal(nextAnchorId, facility.LocationAnchorId);
            Assert.Equal(80, facility.DailyPatientCapacity);
            Assert.False(facility.IsActive);
            Assert.Equal(8, facility.LastSourceRevision);
        }

        [Fact]
        public void SynchronizeProvisioning_WhenRevisionIsStale_KeepsCurrentFacts()
        {
            CareFacility facility = CreateFacility();

            bool changed = facility.TrySynchronizeProvisioning(
                simulationHostId: HostId,
                name: "Stale Clinic",
                kind: new CareFacilityKindKey("PrimaryCare"),
                locationAnchorId: null,
                dailyPatientCapacity: 10,
                isActive: false,
                sourceRevision: 7,
                synchronizedAtUtc: SynchronizedAtUtc.AddMinutes(1));

            Assert.False(changed);
            Assert.Equal("Central Hospital", facility.Name);
            Assert.Equal(240, facility.DailyPatientCapacity);
            Assert.True(facility.IsActive);
        }

        [Fact]
        public void Register_WhenCapacityIsNotPositive_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CareFacility.Register(
                id: FacilityId,
                simulationHostId: HostId,
                name: "Central Hospital",
                kind: new CareFacilityKindKey("Hospital"),
                locationAnchorId: null,
                dailyPatientCapacity: 0,
                isActive: true,
                sourceRevision: 7,
                synchronizedAtUtc: SynchronizedAtUtc));
        }

        private static CareFacility CreateFacility()
        {
            return CareFacility.Register(
                id: FacilityId,
                simulationHostId: HostId,
                name: "Central Hospital",
                kind: new CareFacilityKindKey("Hospital"),
                locationAnchorId: null,
                dailyPatientCapacity: 240,
                isActive: true,
                sourceRevision: 7,
                synchronizedAtUtc: SynchronizedAtUtc);
        }
    }
}
