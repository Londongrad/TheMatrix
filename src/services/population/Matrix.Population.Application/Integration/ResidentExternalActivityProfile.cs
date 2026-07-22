using Matrix.Population.Domain.Models;

namespace Matrix.Population.Application.Integration
{
    public sealed record ResidentExternalActivityProfile(
        long ResidentLifecycleRevision,
        PersonRoutineProfile Routine,
        Guid? DestinationAnchorId,
        string? CommutePurpose,
        ResidentWorkforceQualificationTier WorkforceQualification,
        ResidentExternalEconomicProfile? Economics = null)
    {
        public PersonRoutineProfile Routine { get; } = Routine ?? throw new ArgumentNullException(nameof(Routine));
        public ResidentExternalEconomicProfile Economics { get; } = Economics ?? ResidentExternalEconomicProfile.Neutral;

        public bool HasStructuredActivity => Routine.HasStructuredActivity;

        public static ResidentExternalActivityProfile None { get; } = new(
            ResidentLifecycleRevision: 0,
            Routine: PersonRoutineProfile.Unstructured,
            DestinationAnchorId: null,
            CommutePurpose: null,
            WorkforceQualification: ResidentWorkforceQualificationTier.None);
    }
}
