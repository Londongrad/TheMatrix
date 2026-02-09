using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Errors;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects
{
    /// <summary>
    ///     Wind speed in kilometers per hour.
    /// </summary>
    public readonly record struct WindSpeedKph
    {
        public const decimal Min = 0m;
        public const decimal Max = 400m;

        public WindSpeedKph(decimal value)
        {
            Value = GuardHelper.AgainstOutOfRange(
                value: value,
                min: Min,
                max: Max,
                errorFactory: DomainErrorsFactory.WindSpeedKphOutOfRange,
                propertyName: nameof(Value));
        }

        public decimal Value { get; }

        public static WindSpeedKph From(decimal value)
        {
            return new WindSpeedKph(value);
        }

        public override string ToString()
        {
            return Value.ToString("0.##");
        }
    }
}
