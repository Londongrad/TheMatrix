namespace Matrix.Population.Domain.Models
{
    public sealed record PersonRoutineProfile
    {
        private PersonRoutineProfile(
            TimeSpan? structuredActivityStart,
            TimeSpan? structuredActivityEnd,
            PersonStructuredActivityLoad? structuredActivityLoad,
            PersonRoutineDays activityDays = PersonRoutineDays.None)
        {
            StructuredActivityStart = structuredActivityStart;
            StructuredActivityEnd = structuredActivityEnd;
            StructuredActivityLoad = structuredActivityLoad;
            ActivityDays = activityDays;
        }

        public static PersonRoutineProfile Unstructured { get; } = new(
            structuredActivityStart: null,
            structuredActivityEnd: null,
            structuredActivityLoad: null);

        public TimeSpan? StructuredActivityStart { get; }
        public TimeSpan? StructuredActivityEnd { get; }
        public PersonStructuredActivityLoad? StructuredActivityLoad { get; }
        public PersonRoutineDays ActivityDays { get; }
        public bool HasStructuredActivity => StructuredActivityStart.HasValue;

        public bool IsScheduledOn(DayOfWeek day) =>
            day is >= DayOfWeek.Sunday and <= DayOfWeek.Saturday && ((int)ActivityDays & (1 << (int)day)) != 0;

        public static PersonRoutineProfile Structured(
            TimeSpan activityStart,
            TimeSpan activityEnd,
            PersonStructuredActivityLoad activityLoad,
            PersonRoutineDays activityDays = PersonRoutineDays.Weekdays)
        {
            if (activityStart < TimeSpan.Zero || activityStart >= TimeSpan.FromDays(1))
                throw new ArgumentOutOfRangeException(nameof(activityStart));
            if (activityEnd <= activityStart || activityEnd > TimeSpan.FromDays(1))
                throw new ArgumentOutOfRangeException(nameof(activityEnd));
            if (!Enum.IsDefined(activityLoad))
                throw new ArgumentOutOfRangeException(nameof(activityLoad));
            if (activityDays == PersonRoutineDays.None || (activityDays & ~PersonRoutineDays.EveryDay) != 0)
                throw new ArgumentOutOfRangeException(nameof(activityDays));

            return new PersonRoutineProfile(
                structuredActivityStart: activityStart,
                structuredActivityEnd: activityEnd,
                structuredActivityLoad: activityLoad,
                activityDays: activityDays);
        }
    }

    public enum PersonStructuredActivityLoad
    {
        Moderate = 1,
        Demanding = 2
    }

    [Flags]
    public enum PersonRoutineDays
    {
        None = 0,
        Sunday = 1,
        Monday = 2,
        Tuesday = 4,
        Wednesday = 8,
        Thursday = 16,
        Friday = 32,
        Saturday = 64,
        Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
        EveryDay = Weekdays | Saturday | Sunday
    }
}
