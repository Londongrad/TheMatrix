using Matrix.Population.Domain.Errors;

namespace Matrix.Population.Domain.ValueObjects
{
    /// <summary>
    /// Represents a person's life span (birth and optional death date).
    /// </summary>
    public sealed record LifeSpan
    {
        public DateOnly BirthDate { get; }
        public DateOnly? DeathDate { get; }

        private LifeSpan() { }

        private LifeSpan(DateOnly birthDate, DateOnly? deathDate)
        {
            if (deathDate is not null && deathDate < birthDate)
            {
                throw PopulationErrors.DeathCannotBeEarlierThenBirth(nameof(deathDate));
            }

            BirthDate = birthDate;
            DeathDate = deathDate;
        }

        /// <summary>
        /// Creates a life span for a living person without a death date.
        /// </summary>
        public static LifeSpan FromBirthDate(DateOnly birthDate) =>
            new(birthDate, deathDate: null);

        /// <summary>
        /// Creates a life span from explicit dates (mostly for persistence).
        /// </summary>
        public static LifeSpan FromDates(DateOnly birthDate, DateOnly? deathDate) =>
            new(birthDate, deathDate);

        /// <summary>
        /// Marks the person as dead at the given date.
        /// </summary>
        public LifeSpan WithDeath(DateOnly deathDate)
        {
            if (DeathDate is not null)
            {
                throw new DomainValidationException(
                    "Person is already dead.",
                    nameof(DeathDate));
            }

            return new LifeSpan(BirthDate, deathDate);
        }

        /// <summary>
        /// Removes the death date and makes the person alive again.
        /// </summary>
        public LifeSpan Resurrect()
        {
            if (DeathDate is null)
            {
                throw new DomainValidationException(
                    "Person is already alive.",
                    nameof(DeathDate));
            }

            return new LifeSpan(BirthDate, deathDate: null);
        }

        /// <summary>
        /// Age at the given simulation date.
        /// For deceased persons age is calculated at the moment of death.
        /// </summary>
        public Age GetAge(DateOnly currentDate)
        {
            var effectiveDate = DeathDate ?? currentDate;
            return Age.FromBirthDate(BirthDate, effectiveDate);
        }
    }
}
