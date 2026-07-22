namespace Matrix.Population.Application.Integration.Education
{
    public static class EducationResidentExternalActivityProfileFactory
    {
        public static ResidentExternalActivityProfile Create(
            EducationParticipationProjection? participation)
        {
            if (participation is null)
                return ResidentExternalActivityProfile.None;

            return new ResidentExternalActivityProfile(
                HasStructuredActivity: participation.IsEnrolled,
                DestinationAnchorId: participation.IsEnrolled
                    ? participation.InstitutionAnchorId
                    : null,
                CommutePurpose: participation.IsEnrolled
                    ? "EducationCommute"
                    : null,
                WorkforceQualification: MapQualification(participation.CompletedStage));
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
