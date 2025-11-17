using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Exceptions;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct Age
    {
        public const int MinYears = 0;
        public const int MaxYears = 120;

        public int Years { get; }

        private Age(int years)
        {
            Years = GuardHelper.AgainstOutOfRange(
                years,
                MinYears,
                MaxYears,
                nameof(Age));
        }

        public static Age FromYears(int years) => new(years);

        public static Age FromBirthDate(DateOnly birthDate, DateOnly currentDate)
        {
            if (currentDate < birthDate)
                throw new DomainValidationException(
                    "Current date cannot be earlier than birth date.",
                    nameof(currentDate));

            var years = currentDate.Year - birthDate.Year;
            if (currentDate < birthDate.AddYears(years))
                years--;

            return new Age(years);
        }

        /// <summary>
        /// Returns a new Age with increased years.
        /// </summary>
        public Age AddYears(int years)
        {
            if (years <= 0)
                throw new DomainValidationException(
                    "Age increment must be positive.",
                    nameof(years));

            var newYears = Years + years;
            if (newYears > MaxYears)
                throw new DomainValidationException(
                    $"Age cannot exceed {MaxYears} years.",
                    nameof(years));

            return new Age(newYears);
        }
    }
}
