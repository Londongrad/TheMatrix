using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Exceptions;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed class Age(int years)
    {
        public int Years { get; } = GuardHelper.AgainstOutOfRange(years, 0, 120, nameof(Age));

        public Age Increase(int years)
        {
            if (years <= 0)
                throw new DomainValidationException("Age increment must be positive.");

            var newYears = Years + years;

            if (newYears > 120)
                throw new DomainValidationException("Age cannot exceed 120 years.");

            return new Age(newYears);
        }
    }
}
