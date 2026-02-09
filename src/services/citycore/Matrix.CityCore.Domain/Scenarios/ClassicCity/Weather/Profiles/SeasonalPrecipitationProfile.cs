using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Errors;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.Profiles
{
    /// <summary>
    ///     Seasonal precipitation baseline used by climate evaluation services.
    /// </summary>
    public sealed record class SeasonalPrecipitationProfile
    {
        private SeasonalPrecipitationProfile() { }

        private SeasonalPrecipitationProfile(
            HumidityPercent springHumidity,
            HumidityPercent summerHumidity,
            HumidityPercent autumnHumidity,
            HumidityPercent winterHumidity,
            PrecipitationKind springDominantKind,
            PrecipitationKind summerDominantKind,
            PrecipitationKind autumnDominantKind,
            PrecipitationKind winterDominantKind)
        {
            SpringHumidity = springHumidity;
            SummerHumidity = summerHumidity;
            AutumnHumidity = autumnHumidity;
            WinterHumidity = winterHumidity;
            SpringDominantKind = springDominantKind;
            SummerDominantKind = summerDominantKind;
            AutumnDominantKind = autumnDominantKind;
            WinterDominantKind = winterDominantKind;
        }

        public HumidityPercent SpringHumidity { get; }
        public HumidityPercent SummerHumidity { get; }
        public HumidityPercent AutumnHumidity { get; }
        public HumidityPercent WinterHumidity { get; }

        public PrecipitationKind SpringDominantKind { get; }
        public PrecipitationKind SummerDominantKind { get; }
        public PrecipitationKind AutumnDominantKind { get; }
        public PrecipitationKind WinterDominantKind { get; }

        public static SeasonalPrecipitationProfile Create(
            HumidityPercent springHumidity,
            HumidityPercent summerHumidity,
            HumidityPercent autumnHumidity,
            HumidityPercent winterHumidity,
            PrecipitationKind springDominantKind,
            PrecipitationKind summerDominantKind,
            PrecipitationKind autumnDominantKind,
            PrecipitationKind winterDominantKind)
        {
            GuardHelper.AgainstInvalidEnum(
                value: springDominantKind,
                propertyName: nameof(SpringDominantKind));
            GuardHelper.AgainstInvalidEnum(
                value: summerDominantKind,
                propertyName: nameof(SummerDominantKind));
            GuardHelper.AgainstInvalidEnum(
                value: autumnDominantKind,
                propertyName: nameof(AutumnDominantKind));
            GuardHelper.AgainstInvalidEnum(
                value: winterDominantKind,
                propertyName: nameof(WinterDominantKind));

            return new SeasonalPrecipitationProfile(
                springHumidity: springHumidity,
                summerHumidity: summerHumidity,
                autumnHumidity: autumnHumidity,
                winterHumidity: winterHumidity,
                springDominantKind: springDominantKind,
                summerDominantKind: summerDominantKind,
                autumnDominantKind: autumnDominantKind,
                winterDominantKind: winterDominantKind);
        }

        public HumidityPercent GetHumidity(WeatherSeason season)
        {
            return season switch
            {
                WeatherSeason.Spring => SpringHumidity,
                WeatherSeason.Summer => SummerHumidity,
                WeatherSeason.Autumn => AutumnHumidity,
                WeatherSeason.Winter => WinterHumidity,
                _ => throw DomainErrorsFactory.InvalidClimateProfile(
                    reason: "Unknown weather season for precipitation profile humidity.",
                    propertyName: nameof(season))
            };
        }

        public PrecipitationKind GetDominantKind(WeatherSeason season)
        {
            return season switch
            {
                WeatherSeason.Spring => SpringDominantKind,
                WeatherSeason.Summer => SummerDominantKind,
                WeatherSeason.Autumn => AutumnDominantKind,
                WeatherSeason.Winter => WinterDominantKind,
                _ => throw DomainErrorsFactory.InvalidClimateProfile(
                    reason: "Unknown weather season for precipitation profile kind.",
                    propertyName: nameof(season))
            };
        }
    }
}
