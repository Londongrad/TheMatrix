using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Errors;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities.Enums;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities
{
    /// <summary>
    ///     Long-lived city generation profile that drives deterministic world bootstrap.
    /// </summary>
    public sealed record class CityGenerationProfile
    {
        public const int MaxPlannedPeopleCount = 1_000_000;

        private CityGenerationProfile() { }

        private CityGenerationProfile(
            CitySizeTier sizeTier,
            UrbanDensity urbanDensity,
            CityDevelopmentLevel developmentLevel,
            PopulationOccupancyProfile populationOccupancyProfile,
            int? plannedPeopleCount)
        {
            SizeTier = sizeTier;
            UrbanDensity = urbanDensity;
            DevelopmentLevel = developmentLevel;
            PopulationOccupancyProfile = populationOccupancyProfile;
            PlannedPeopleCount = plannedPeopleCount;
        }

        public CitySizeTier SizeTier { get; private set; }
        public UrbanDensity UrbanDensity { get; private set; }
        public CityDevelopmentLevel DevelopmentLevel { get; private set; }
        public PopulationOccupancyProfile PopulationOccupancyProfile { get; private set; }
        public int? PlannedPeopleCount { get; private set; }

        public static CityGenerationProfile Create(
            CitySizeTier sizeTier,
            UrbanDensity urbanDensity,
            CityDevelopmentLevel developmentLevel,
            PopulationOccupancyProfile populationOccupancyProfile,
            int? plannedPeopleCount = null)
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

            GuardHelper.AgainstInvalidEnum(
                value: populationOccupancyProfile,
                propertyName: nameof(PopulationOccupancyProfile));

            if (plannedPeopleCount is < 0 or > MaxPlannedPeopleCount)
                throw ClassicCityDomainErrorsFactory.InvalidCityGenerationProfile(
                    reason:
                    $"Planned people count must stay between 0 and {MaxPlannedPeopleCount}.",
                    propertyName: nameof(PlannedPeopleCount));

            return new CityGenerationProfile(
                sizeTier: sizeTier,
                urbanDensity: urbanDensity,
                developmentLevel: developmentLevel,
                populationOccupancyProfile: populationOccupancyProfile,
                plannedPeopleCount: plannedPeopleCount);
        }
    }
}
