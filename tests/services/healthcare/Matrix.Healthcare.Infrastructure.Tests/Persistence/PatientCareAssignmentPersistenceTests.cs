using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Matrix.Healthcare.Infrastructure.Persistence;
using Matrix.Healthcare.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Matrix.Healthcare.Infrastructure.Tests.Persistence;

public sealed class PatientCareAssignmentPersistenceTests
{
    [Fact]
    public async Task SaveAndReload_PreservesDeliveredTreatmentAudit()
    {
        await using HealthcareDbContext dbContext =
            HealthcareInfrastructureTestSupport.CreateDbContext();
        var hostId = new SimulationHostId(Guid.NewGuid());
        var facilityId = new CareFacilityId(Guid.NewGuid());
        DateTimeOffset assignedAtUtc =
            DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");
        DateOnly careDate = new(2048, 5, 7);
        CareFacility facility = CareFacility.Register(
            facilityId,
            hostId,
            "Central Hospital",
            new CareFacilityKindKey("Hospital"),
            locationAnchorId: null,
            dailyPatientCapacity: 20,
            isActive: true,
            sourceRevision: 7,
            synchronizedAtUtc: assignedAtUtc);
        PatientCareAssignment assignment = PatientCareAssignment.Assign(
            PatientCareAssignmentId.New(),
            hostId,
            new PatientId(Guid.NewGuid()),
            facilityId,
            careDate,
            CareNeedUrgency.Acute,
            assessmentRevision: 17,
            lifecycleRevision: 2,
            assignedAtUtc: assignedAtUtc);
        assignment.TryMarkDelivered(
            careDate,
            assignedAtUtc.AddDays(1),
            treatmentHealthDelta: 6,
            treatmentMedicalStateChanged: true);
        dbContext.CareFacilities.Add(facility);
        dbContext.PatientCareAssignments.Add(assignment);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        PatientCareAssignment? loaded = await dbContext.PatientCareAssignments.FindAsync(
            assignment.PatientCareAssignmentId);

        Assert.NotNull(loaded);
        Assert.Equal(PatientCareAssignmentStatus.Delivered, loaded.Status);
        Assert.Equal(careDate, loaded.ClosedOn);
        Assert.Equal(assignedAtUtc.AddDays(1), loaded.ClosedAtUtc);
        Assert.Equal(6, loaded.TreatmentHealthDelta);
        Assert.True(loaded.TreatmentMedicalStateChanged);
        Assert.Null(loaded.CancellationReason);
    }
}
