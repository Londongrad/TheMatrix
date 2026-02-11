using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects
{
    /// <summary>
    ///     Normalized volatility factor in the range [0..1].
    /// </summary>
    public readonly record struct WeatherVolatility
    {
        public const decimal Min = 0m;
        public const decimal Max = 1m;

        public WeatherVolatility(decimal value)
        {
            Value = GuardHelper.AgainstOutOfRange(
                value: value,
                min: Min,
                max: Max,
                errorFactory: ClassicCityDomainErrorsFactory.WeatherVolatilityOutOfRange,
                propertyName: nameof(Value));
        }

        public decimal Value { get; }

        public static WeatherVolatility From(decimal value)
        {
            return new WeatherVolatility(value);
        }

        public override string ToString()
        {
            return Value.ToString("0.###");
        }
    }
}
