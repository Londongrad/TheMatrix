using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities
{
    public sealed class CityInitialWeatherProfile
    {
        private CityInitialWeatherProfile(
            InitialWeatherMode mode,
            WeatherType? manualType,
            WeatherSeverity? manualSeverity,
            TemperatureC? manualTemperature)
        {
            Mode = GuardHelper.AgainstInvalidEnum(
                value: mode,
                propertyName: nameof(mode));
            ManualType = manualType;
            ManualSeverity = manualSeverity;
            ManualTemperature = manualTemperature;
        }

        private CityInitialWeatherProfile()
        {
            Mode = InitialWeatherMode.Random;
        }

        public InitialWeatherMode Mode { get; private set; }
        public WeatherType? ManualType { get; private set; }
        public WeatherSeverity? ManualSeverity { get; private set; }
        public TemperatureC? ManualTemperature { get; private set; }

        public static CityInitialWeatherProfile CreateRandom()
        {
            return new CityInitialWeatherProfile(
                mode: InitialWeatherMode.Random,
                manualType: null,
                manualSeverity: null,
                manualTemperature: null);
        }

        public static CityInitialWeatherProfile CreateManual(
            WeatherType manualType,
            WeatherSeverity manualSeverity,
            TemperatureC? manualTemperature = null)
        {
            GuardHelper.AgainstInvalidEnum(
                value: manualType,
                propertyName: nameof(manualType));
            GuardHelper.AgainstInvalidEnum(
                value: manualSeverity,
                propertyName: nameof(manualSeverity));

            return new CityInitialWeatherProfile(
                mode: InitialWeatherMode.Manual,
                manualType: manualType,
                manualSeverity: manualSeverity,
                manualTemperature: manualTemperature);
        }
    }
}
