namespace Matrix.Population.Application.Integration
{
    public sealed record ResidentExternalActivityProfile(
        bool HasStructuredActivity,
        Guid? DestinationAnchorId,
        string? CommutePurpose,
        ResidentWorkforceQualificationTier WorkforceQualification)
    {
        public static ResidentExternalActivityProfile None { get; } = new(
            HasStructuredActivity: false,
            DestinationAnchorId: null,
            CommutePurpose: null,
            WorkforceQualification: ResidentWorkforceQualificationTier.None);
    }
}
