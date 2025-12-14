using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Errors;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct Age
    {
        public const int MinYears = 0;
        public const int MaxYears = 120;

        private Age(int years)
        {
            Years = GuardHelper.AgainstOutOfRange(
                value: years,
                min: MinYears,
                max: MaxYears,
                propertyName: nameof(Age));
        }

        public int Years { get; }

        public static Age FromYears(int years)
        {
            return new Age(years);
        }

        public static Age FromBirthDate(
            DateOnly birthDate,
            DateOnly currentDate)
        {
            if (currentDate < birthDate)
                throw DomainErrorsFactory.CurrentDateLessThanBirth();

            int years = currentDate.Year - birthDate.Year;
            if (currentDate < birthDate.AddYears(years))
                years--;

            return new Age(years);
        }

        /// <summary>
        ///     Returns a new Age with increased years.
        /// </summary>
        public Age AddYears(int years)
        {
            if (years <= 0)
                throw DomainErrorsFactory.AgeIncrementMustBePositive(nameof(years));

            int newYears = Years + years;
            if (newYears > MaxYears)
                throw DomainErrorsFactory.AgeCannotExceedMaxYears(
                    maxYears: MaxYears,
                    propertyName: nameof(years));

            return new Age(newYears);
        }
    }
}
