using Matrix.Education.Application.Abstractions;
using Matrix.Education.Application.Students.GetStudentEducationStatus;
using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Education.Infrastructure.Persistence.Queries;

public sealed class StudentEducationStatusReader(EducationDbContext dbContext)
    : IStudentEducationStatusReader
{
    public async Task<StudentEducationStatusView?> GetAsync(
        SimulationHostId simulationHostId,
        ResidentId residentId,
        CancellationToken cancellationToken = default)
    {
        var row = await (
                from profile in dbContext.StudentProfiles.AsNoTracking()
                where profile.SimulationHostId == simulationHostId
                      && profile.ResidentId == residentId
                join enrollment in dbContext.Enrollments
                       .AsNoTracking()
                       .Where(candidate => candidate.Status == EnrollmentStatus.Active)
                    on new { profile.SimulationHostId, profile.ResidentId }
                    equals new { enrollment.SimulationHostId, enrollment.ResidentId }
                    into enrollments
                from enrollment in enrollments.DefaultIfEmpty()
                join institution in dbContext.Institutions.AsNoTracking()
                    on enrollment.InstitutionId equals institution.EducationInstitutionId
                    into institutions
                from institution in institutions.DefaultIfEmpty()
                select new
                {
                    Profile = profile,
                    Enrollment = enrollment,
                    Institution = institution
                })
           .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
            return null;

        ActiveStudentEnrollmentView? activeEnrollment = null;
        if (row.Enrollment is not null)
        {
            if (row.Institution is null)
                throw new InvalidOperationException(
                    "An active education enrollment references a missing institution.");

            activeEnrollment = new ActiveStudentEnrollmentView(
                EnrollmentId: row.Enrollment.EnrollmentId.Value,
                InstitutionId: row.Institution.EducationInstitutionId.Value,
                InstitutionName: row.Institution.Name,
                InstitutionKind: row.Institution.Kind.Value,
                LocationAnchorId: row.Institution.LocationAnchorId?.Value,
                Stage: row.Enrollment.Stage.Value,
                EnrolledOn: row.Enrollment.EnrolledOn);
        }

        return new StudentEducationStatusView(
            ResidentId: row.Profile.ResidentId.Value,
            IsAlive: row.Profile.IsAlive,
            IsActive: row.Profile.IsActive,
            CompletedStage: row.Profile.CompletedStage?.Value,
            CompletedStageOn: row.Profile.CompletedStageOn,
            ActiveEnrollment: activeEnrollment);
    }
}
