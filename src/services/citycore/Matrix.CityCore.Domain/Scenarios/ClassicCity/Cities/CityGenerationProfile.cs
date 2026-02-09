using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities.Enums;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities
{
    /// <summary>
    ///     Long-lived city generation profile that drives deterministic world bootstrap.
    /// </summary>
    public sealed record class CityGenerationProfile
    {
        private CityGenerationProfile() { }

        private CityGenerationProfile(
            CitySizeTier sizeTier,
            UrbanDensity urbanDensity,
            CityDevelopmentLevel developmentLevel)
        {
            SizeTier = sizeTier;
            UrbanDensity = urbanDensity;
            DevelopmentLevel = developmentLevel;
        }

        public CitySizeTier SizeTier { get; private set; }
        public UrbanDensity UrbanDensity { get; private set; }
        public CityDevelopmentLevel DevelopmentLevel { get; private set; }

        public static CityGenerationProfile Create(
            CitySizeTier sizeTier,
            UrbanDensity urbanDensity,
            CityDevelopmentLevel developmentLevel)
        {
            GuardHelper.AgainstInvalidEnum(
                value: sizeTier,
                propertyName: nameof(SizeTier));

            GuardHelper.AgainstInvalidEnum(
                value: urbanDensity,
                propertyName: nameof(UrbanDensity));

            GuardHelper.AgainstInvalidEnum(
                value: developmentLevel,
                propertyName: nameof(DevelopmentLevel));

            return new CityGenerationProfile(
                sizeTier: sizeTier,
                urbanDensity: urbanDensity,
                developmentLevel: developmentLevel);
        }
    }
}
