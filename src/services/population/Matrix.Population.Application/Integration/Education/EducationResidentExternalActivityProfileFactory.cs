using Matrix.Population.Domain.Models;

namespace Matrix.Population.Application.Integration.Education
{
    public static class EducationResidentExternalActivityProfileFactory
    {
        public static ResidentExternalActivityProfile Create(
            EducationParticipationProjection? participation)
        {
            if (participation is null)
                return ResidentExternalActivityProfile.None;

            ResidentWorkforceQualificationTier qualification = MapQualification(participation.CompletedStage);
            PersonRoutineProfile routine = participation.IsEnrolled
                ? participation.Routine ?? LegacyEducationResidentRoutineProfile.Enrolled
                : PersonRoutineProfile.Unstructured;
            return new ResidentExternalActivityProfile(
                ResidentLifecycleRevision: participation.ResidentLifecycleRevision,
                Routine: routine,
                DestinationAnchorId: routine.HasStructuredActivity
                    ? participation.InstitutionAnchorId
                    : null,
                CommutePurpose: routine.HasStructuredActivity
                    ? "EducationCommute"
                    : null,
                WorkforceQualification: qualification,
                Economics: participation.Economics
                    ?? LegacyEducationResidentEconomicProfileFactory.Resolve(participation.IsEnrolled, qualification));
        }

        private static ResidentWorkforceQualificationTier MapQualification(string? completedStage)
        {
            return completedStage switch
            {
                "primary" => ResidentWorkforceQualificationTier.Entry,
                "lower-secondary" => ResidentWorkforceQualificationTier.Basic,
                "upper-secondary" => ResidentWorkforceQualificationTier.General,
                "vocational" => ResidentWorkforceQualificationTier.Skilled,
                "higher" or "higher-education" => ResidentWorkforceQualificationTier.Professional,
                "postgraduate" => ResidentWorkforceQualificationTier.Specialist,
                _ => ResidentWorkforceQualificationTier.None
            };
        }
    }
}
