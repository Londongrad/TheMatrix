using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities
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
            CityEconomyProfile economyProfile,
            PopulationOccupancyProfile populationOccupancyProfile,
            int? plannedPeopleCount)
        {
            SizeTier = sizeTier;
            UrbanDensity = urbanDensity;
            DevelopmentLevel = developmentLevel;
            EconomyProfile = economyProfile;
            PopulationOccupancyProfile = populationOccupancyProfile;
            PlannedPeopleCount = plannedPeopleCount;
        }

        public CitySizeTier SizeTier { get; }
        public UrbanDensity UrbanDensity { get; }
        public CityDevelopmentLevel DevelopmentLevel { get; }
        public CityEconomyProfile EconomyProfile { get; }
        public PopulationOccupancyProfile PopulationOccupancyProfile { get; }
        public int? PlannedPeopleCount { get; private set; }

        public static CityGenerationProfile Create(
            CitySizeTier sizeTier,
            UrbanDensity urbanDensity,
            CityDevelopmentLevel developmentLevel,
            CityEconomyProfile economyProfile,
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
                value: economyProfile,
                propertyName: nameof(EconomyProfile));

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
                economyProfile: economyProfile,
                populationOccupancyProfile: populationOccupancyProfile,
                plannedPeopleCount: plannedPeopleCount);
        }

        public CityGenerationProfile WithPlannedPeopleCount(int? plannedPeopleCount)
        {
            return Create(
                sizeTier: SizeTier,
                urbanDensity: UrbanDensity,
                developmentLevel: DevelopmentLevel,
                economyProfile: EconomyProfile,
                populationOccupancyProfile: PopulationOccupancyProfile,
                plannedPeopleCount: plannedPeopleCount);
        }
    }
}
