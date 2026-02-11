using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects
{
    /// <summary>
    ///     Cloud coverage percentage in the range [0..100].
    /// </summary>
    public readonly record struct CloudCoveragePercent
    {
        public const decimal Min = 0m;
        public const decimal Max = 100m;

        public CloudCoveragePercent(decimal value)
        {
            Value = GuardHelper.AgainstOutOfRange(
                value: value,
                min: Min,
                max: Max,
                errorFactory: ClassicCityDomainErrorsFactory.CloudCoveragePercentOutOfRange,
                propertyName: nameof(Value));
        }

        public decimal Value { get; }

        public static CloudCoveragePercent From(decimal value)
        {
            return new CloudCoveragePercent(value);
        }

        public override string ToString()
        {
            return Value.ToString("0.##");
        }
    }
}
