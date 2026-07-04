using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;
using Matrix.Healthcare.Infrastructure.Persistence;
using Matrix.Healthcare.Infrastructure.Persistence.Repositories;
using Matrix.Healthcare.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.Healthcare.Infrastructure.Tests.Persistence.Repositories;

public sealed class PatientCareAssignmentRepositoryTests
{
    private static readonly SimulationHostId HostId = new(
        Guid.Parse("10000000-0000-0000-0000-000000000001"));
    private static readonly SimulationHostId ForeignHostId = new(
        Guid.Parse("10000000-0000-0000-0000-000000000002"));
    private static readonly CareFacilityId FacilityId = new(
        Guid.Parse("30000000-0000-0000-0000-000000000001"));
    private static readonly DateOnly DueDate = new(2048, 5, 7);
    private static readonly DateTimeOffset AssignedAtUtc =
        DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");

    [Fact]
    public async Task GetDueScheduledByPatientIds_ReturnsEarliestTrackedAssignmentPerPatient()
    {
        await using HealthcareDbContext dbContext =
            HealthcareInfrastructureTestSupport.CreateDbContext();
        var firstPatientId = new PatientId(
            Guid.Parse("20000000-0000-0000-0000-000000000001"));
        var secondPatientId = new PatientId(
            Guid.Parse("20000000-0000-0000-0000-000000000002"));
        PatientCareAssignment earliest = CreateAssignment(
            HostId,
            firstPatientId,
            DueDate.AddDays(-1));
        PatientCareAssignment laterForSamePatient = CreateAssignment(
            HostId,
            firstPatientId,
            DueDate);
        PatientCareAssignment secondPatient = CreateAssignment(
            HostId,
            secondPatientId,
            DueDate);
        PatientCareAssignment future = CreateAssignment(
            HostId,
            new PatientId(Guid.NewGuid()),
            DueDate.AddDays(1));
        PatientCareAssignment cancelled = CreateAssignment(
            HostId,
            new PatientId(Guid.NewGuid()),
            DueDate);
        cancelled.TryCancel(
            DueDate,
            AssignedAtUtc.AddHours(1),
            PatientCareAssignmentCancellationReason.CareNoLongerRequired);
        PatientCareAssignment foreign = CreateAssignment(
            ForeignHostId,
            firstPatientId,
            DueDate);
        dbContext.PatientCareAssignments.AddRange(
            laterForSamePatient,
            secondPatient,
            future,
            cancelled,
            foreign,
            earliest);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var repository = new PatientCareAssignmentRepository(dbContext);

        IReadOnlyList<PatientCareAssignment> assignments =
            await repository.GetDueScheduledByPatientIdsAsync(
                HostId,
                [firstPatientId, secondPatientId],
                DueDate);

        Assert.Equal(
            [earliest.PatientCareAssignmentId, secondPatient.PatientCareAssignmentId],
            assignments.Select(assignment => assignment.PatientCareAssignmentId));
        Assert.All(assignments, assignment => Assert.Equal(
            EntityState.Unchanged,
            dbContext.Entry(assignment).State));
    }

    [Fact]
    public void DueScheduledQuery_TranslatesSelectionPerPatientToPostgreSql()
    {
        DbContextOptions<HealthcareDbContext> options =
            new DbContextOptionsBuilder<HealthcareDbContext>()
               .UseNpgsql("Host=localhost;Database=healthcare_translation_test;Username=test;Password=test")
               .Options;
        using var dbContext = new HealthcareDbContext(options);
        var repository = new PatientCareAssignmentRepository(dbContext);

        string sql = repository.BuildDueScheduledQuery(
                HostId,
                [new PatientId(Guid.NewGuid())],
                DueDate)
           .ToQueryString();

        Assert.Contains("NOT EXISTS", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("care_date", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("status", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static PatientCareAssignment CreateAssignment(
        SimulationHostId hostId,
        PatientId patientId,
        DateOnly careDate)
    {
        return PatientCareAssignment.Assign(
            PatientCareAssignmentId.New(),
            hostId,
            patientId,
            FacilityId,
            careDate,
            CareNeedUrgency.Urgent,
            assessmentRevision: 17,
            lifecycleRevision: 0,
            assignedAtUtc: AssignedAtUtc);
    }
}
