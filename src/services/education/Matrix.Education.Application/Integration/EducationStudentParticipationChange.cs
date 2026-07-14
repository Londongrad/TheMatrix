using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Students;

namespace Matrix.Education.Application.Integration
{
    public sealed record EducationStudentParticipationChange(
        Guid ResidentId,
        long ParticipationRevision,
        long ResidentLifecycleRevision,
        bool IsEnrolled,
        string? ActiveStage,
        Guid? InstitutionId,
        Guid? InstitutionAnchorId,
        DateOnly? EnrolledOn,
        string? CompletedStage,
        DateOnly? CompletedStageOn)
    {
        public static EducationStudentParticipationChange Capture(
            StudentProfile profile,
            StudentEnrollment? activeEnrollment = null,
            EducationInstitution? institution = null)
        {
            ArgumentNullException.ThrowIfNull(profile);

            if (activeEnrollment is not null)
            {
                if (!activeEnrollment.IsActive)
                    throw new ArgumentException(
                        message: "Only an active enrollment can be captured as current participation.",
                        paramName: nameof(activeEnrollment));
                if (activeEnrollment.SimulationHostId != profile.SimulationHostId
                    || activeEnrollment.ResidentId != profile.ResidentId)
                    throw new ArgumentException(
                        message: "The enrollment does not belong to the student profile.",
                        paramName: nameof(activeEnrollment));
                if (institution is null
                    || institution.SimulationHostId != profile.SimulationHostId
                    || institution.EducationInstitutionId != activeEnrollment.InstitutionId)
                    throw new ArgumentException(
                        message: "The active enrollment requires its matching institution.",
                        paramName: nameof(institution));
            }
            else if (institution is not null)
            {
                throw new ArgumentException(
                    message: "An institution cannot be captured without an active enrollment.",
                    paramName: nameof(institution));
            }

            return new EducationStudentParticipationChange(
                ResidentId: profile.ResidentId.Value,
                ParticipationRevision: profile.ParticipationRevision,
                ResidentLifecycleRevision: profile.LastLifecycleRevision,
                IsEnrolled: activeEnrollment is not null,
                ActiveStage: activeEnrollment?.Stage.Value,
                InstitutionId: activeEnrollment?.InstitutionId.Value,
                InstitutionAnchorId: institution?.LocationAnchorId?.Value,
                EnrolledOn: activeEnrollment?.EnrolledOn,
                CompletedStage: profile.CompletedStage?.Value,
                CompletedStageOn: profile.CompletedStageOn);
        }
    }
}
