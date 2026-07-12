using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Programs;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Matrix.Education.Infrastructure.Persistence;
using Matrix.Education.Infrastructure.Persistence.Queries;
using Matrix.Education.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Matrix.Education.Infrastructure.Tests.Persistence.Queries;

public sealed class StudentEducationStatusReaderTests
{
    [Fact]
    public async Task GetAsync_ProjectsProfileEnrollmentAndInstitutionWithoutTracking()
    {
        await using EducationDbContext dbContext =
            EducationInfrastructureTestSupport.CreateDbContext();
        var simulationHostId = new SimulationHostId(Guid.NewGuid());
        var residentId = new ResidentId(Guid.NewGuid());
        var institutionId = new EducationInstitutionId(Guid.NewGuid());
        var locationAnchorId = new LocationAnchorId(Guid.NewGuid());
        StudentProfile profile = StudentProfile.Register(
            residentId: residentId,
            simulationHostId: simulationHostId,
            birthDate: new DateOnly(2030, 3, 14),
            isAlive: true,
            isActive: true,
            sourceRevision: 7,
            synchronizedAtUtc: new DateTimeOffset(2048, 5, 1, 8, 0, 0, TimeSpan.Zero));
        profile.RecordStageCompletion(
            stage: new EducationStageKey("primary"),
            completedOn: new DateOnly(2042, 6, 1));
        EducationInstitution institution = EducationInstitution.Create(
            id: institutionId,
            simulationHostId: simulationHostId,
            name: "Central School",
            kind: new EducationInstitutionKindKey("School"),
            capacity: 500,
            locationAnchorId: locationAnchorId);
        Assert.True(institution.TryReserveSeats(1));
        StudentEnrollment enrollment = StudentEnrollment.Enroll(
            id: new EnrollmentId(Guid.NewGuid()),
            simulationHostId: simulationHostId,
            residentId: residentId,
            institutionId: institutionId,
            stage: new EducationStageKey("secondary"),
            enrolledOn: new DateOnly(2048, 5, 2));
        dbContext.StudentProfiles.Add(profile);
        dbContext.Institutions.Add(institution);
        dbContext.Enrollments.Add(enrollment);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var reader = new StudentEducationStatusReader(dbContext);

        var result = await reader.GetAsync(simulationHostId, residentId);

        Assert.NotNull(result);
        Assert.Equal(residentId.Value, result.ResidentId);
        Assert.Equal("primary", result.CompletedStage);
        Assert.Equal(new DateOnly(2042, 6, 1), result.CompletedStageOn);
        Assert.NotNull(result.ActiveEnrollment);
        Assert.Equal(enrollment.EnrollmentId.Value, result.ActiveEnrollment.EnrollmentId);
        Assert.Equal(institutionId.Value, result.ActiveEnrollment.InstitutionId);
        Assert.Equal("Central School", result.ActiveEnrollment.InstitutionName);
        Assert.Equal("school", result.ActiveEnrollment.InstitutionKind);
        Assert.Equal(locationAnchorId.Value, result.ActiveEnrollment.LocationAnchorId);
        Assert.Equal("secondary", result.ActiveEnrollment.Stage);
        Assert.Empty(dbContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetAsync_WhenProfileDoesNotExist_ReturnsNull()
    {
        await using EducationDbContext dbContext =
            EducationInfrastructureTestSupport.CreateDbContext();
        var reader = new StudentEducationStatusReader(dbContext);

        var result = await reader.GetAsync(
            new SimulationHostId(Guid.NewGuid()),
            new ResidentId(Guid.NewGuid()));

        Assert.Null(result);
    }
}
