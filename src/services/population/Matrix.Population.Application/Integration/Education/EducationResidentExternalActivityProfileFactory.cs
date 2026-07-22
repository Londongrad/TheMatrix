using Matrix.Population.Domain.Models;

namespace Matrix.Population.Application.Integration.Education
{
    public static class EducationResidentExternalActivityProfileFactory
    {
        private static readonly PersonRoutineProfile EducationRoutine = PersonRoutineProfile.Structured(
            activityStart: TimeSpan.FromHours(8),
            activityEnd: TimeSpan.FromHours(15),
            activityLoad: PersonStructuredActivityLoad.Moderate);

        public static ResidentExternalActivityProfile Create(
            EducationParticipationProjection? participation)
        {
            if (participation is null)
                return ResidentExternalActivityProfile.None;

            ResidentWorkforceQualificationTier qualification = MapQualification(participation.CompletedStage);
            return new ResidentExternalActivityProfile(
                ResidentLifecycleRevision: participation.ResidentLifecycleRevision,
                Routine: participation.IsEnrolled ? EducationRoutine : PersonRoutineProfile.Unstructured,
                DestinationAnchorId: participation.IsEnrolled
                    ? participation.InstitutionAnchorId
                    : null,
                CommutePurpose: participation.IsEnrolled
                    ? "EducationCommute"
                    : null,
                WorkforceQualification: qualification,
                Economics: EducationResidentEconomicProfileFactory.Resolve(participation.IsEnrolled, qualification));
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
