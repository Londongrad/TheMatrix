namespace Matrix.Population.Application.Integration
{
    public sealed record ResidentExternalActivityProfile(
        bool HasStructuredActivity,
        Guid? DestinationAnchorId,
        ResidentWorkforceQualificationTier WorkforceQualification)
    {
        public static ResidentExternalActivityProfile None { get; } = new(
            HasStructuredActivity: false,
            DestinationAnchorId: null,
            WorkforceQualification: ResidentWorkforceQualificationTier.None);
    }
}
