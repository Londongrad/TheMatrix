using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects
{
    /// <summary>
    ///     Temperature in degrees Celsius.
    /// </summary>
    public readonly record struct TemperatureC
    {
        public const decimal Min = -100m;
        public const decimal Max = 80m;

        public TemperatureC(decimal value)
        {
            Value = GuardHelper.AgainstOutOfRange(
                value: value,
                min: Min,
                max: Max,
                errorFactory: ClassicCityDomainErrorsFactory.TemperatureCOutOfRange,
                propertyName: nameof(Value));
        }

        public decimal Value { get; }

        public static TemperatureC From(decimal value)
        {
            return new TemperatureC(value);
        }

        public override string ToString()
        {
            return Value.ToString("0.##");
        }
    }
}
