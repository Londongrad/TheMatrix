using Matrix.Population.Domain.Errors;

namespace Matrix.Population.Domain.ValueObjects
{
    /// <summary>
    ///     Represents a person's life span (birth and optional death date).
    /// </summary>
    public sealed record LifeSpan
    {
        private LifeSpan()
        {
        }

        private LifeSpan(DateOnly birthDate, DateOnly? deathDate)
        {
            if (deathDate is not null && deathDate < birthDate)
                throw DomainErrorsFactory.DeathCannotBeEarlierThenBirth(nameof(deathDate));

            BirthDate = birthDate;
            DeathDate = deathDate;
        }

        public DateOnly BirthDate { get; }
        public DateOnly? DeathDate { get; }

        /// <summary>
        ///     Creates a life span for a living person without a death date.
        /// </summary>
        public static LifeSpan FromBirthDate(DateOnly birthDate) =>
            new(birthDate: birthDate, deathDate: null);

        /// <summary>
        ///     Creates a life span from explicit dates (mostly for persistence).
        /// </summary>
        public static LifeSpan FromDates(DateOnly birthDate, DateOnly? deathDate) =>
            new(birthDate: birthDate, deathDate: deathDate);

        public Age GetAge(DateOnly currentDate)
        {
            DateOnly effectiveDate = DeathDate ?? currentDate;
            return Age.FromBirthDate(birthDate: BirthDate, currentDate: effectiveDate);
        }
    }
}
