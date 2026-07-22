using Matrix.Population.Domain.Models;

namespace Matrix.Population.Application.Integration
{
    public sealed record ResidentExternalActivityProfile(
        PersonRoutineProfile Routine,
        Guid? DestinationAnchorId,
        string? CommutePurpose,
        ResidentWorkforceQualificationTier WorkforceQualification)
    {
        public PersonRoutineProfile Routine { get; } = Routine ?? throw new ArgumentNullException(nameof(Routine));

        public bool HasStructuredActivity => Routine.HasStructuredActivity;

        public static ResidentExternalActivityProfile None { get; } = new(
            Routine: PersonRoutineProfile.Unstructured,
            DestinationAnchorId: null,
            CommutePurpose: null,
            WorkforceQualification: ResidentWorkforceQualificationTier.None);
    }
}
