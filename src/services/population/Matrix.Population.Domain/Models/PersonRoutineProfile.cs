namespace Matrix.Population.Domain.Models
{
    public sealed record PersonRoutineProfile
    {
        private PersonRoutineProfile(
            TimeSpan? structuredActivityStart,
            TimeSpan? structuredActivityEnd,
            PersonStructuredActivityLoad? structuredActivityLoad)
        {
            StructuredActivityStart = structuredActivityStart;
            StructuredActivityEnd = structuredActivityEnd;
            StructuredActivityLoad = structuredActivityLoad;
        }

        public static PersonRoutineProfile Unstructured { get; } = new(
            structuredActivityStart: null,
            structuredActivityEnd: null,
            structuredActivityLoad: null);

        public TimeSpan? StructuredActivityStart { get; }
        public TimeSpan? StructuredActivityEnd { get; }
        public PersonStructuredActivityLoad? StructuredActivityLoad { get; }
        public bool HasStructuredActivity => StructuredActivityStart.HasValue;

        public static PersonRoutineProfile Structured(
            TimeSpan activityStart,
            TimeSpan activityEnd,
            PersonStructuredActivityLoad activityLoad)
        {
            if (activityStart < TimeSpan.Zero || activityStart >= TimeSpan.FromDays(1))
                throw new ArgumentOutOfRangeException(nameof(activityStart));
            if (activityEnd <= activityStart || activityEnd > TimeSpan.FromDays(1))
                throw new ArgumentOutOfRangeException(nameof(activityEnd));
            if (!Enum.IsDefined(activityLoad))
                throw new ArgumentOutOfRangeException(nameof(activityLoad));

            return new PersonRoutineProfile(
                structuredActivityStart: activityStart,
                structuredActivityEnd: activityEnd,
                structuredActivityLoad: activityLoad);
        }
    }

    public enum PersonStructuredActivityLoad
    {
        Moderate = 1,
        Demanding = 2
    }
}
