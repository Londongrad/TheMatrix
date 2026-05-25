using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Cities
{
    public sealed class CityGenerationProfileTests
    {
        private const string InvalidEnumErrorCode = "Domain.Guard.InvalidEnum";
        private const string InvalidProfileErrorCode = "SimulationCore.City.GenerationProfile.Invalid";

        [Fact]
        public void Create_WithValidValues_CreatesProfile()
        {
            var profile = CityGenerationProfile.Create(
                sizeTier: CitySizeTier.Medium,
                urbanDensity: UrbanDensity.Balanced,
                developmentLevel: CityDevelopmentLevel.Balanced,
                economyProfile: CityEconomyProfile.Balanced,
                populationOccupancyProfile: PopulationOccupancyProfile.Balanced,
                plannedPeopleCount: 42_000);

            Assert.Equal(
                expected: CitySizeTier.Medium,
                actual: profile.SizeTier);
            Assert.Equal(
                expected: UrbanDensity.Balanced,
                actual: profile.UrbanDensity);
            Assert.Equal(
                expected: CityDevelopmentLevel.Balanced,
                actual: profile.DevelopmentLevel);
            Assert.Equal(
                expected: CityEconomyProfile.Balanced,
                actual: profile.EconomyProfile);
            Assert.Equal(
                expected: PopulationOccupancyProfile.Balanced,
                actual: profile.PopulationOccupancyProfile);
            Assert.Equal(
                expected: 42_000,
                actual: profile.PlannedPeopleCount);
        }

        [Fact]
        public void Create_WithInvalidSizeTier_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => CityGenerationProfile.Create(
                sizeTier: (CitySizeTier)999,
                urbanDensity: UrbanDensity.Balanced,
                developmentLevel: CityDevelopmentLevel.Balanced,
                economyProfile: CityEconomyProfile.Balanced,
                populationOccupancyProfile: PopulationOccupancyProfile.Balanced));

            Assert.Equal(
                expected: InvalidEnumErrorCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "SizeTier",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WithNegativePlannedPeopleCount_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => CityGenerationProfile.Create(
                sizeTier: CitySizeTier.Medium,
                urbanDensity: UrbanDensity.Balanced,
                developmentLevel: CityDevelopmentLevel.Balanced,
                economyProfile: CityEconomyProfile.Balanced,
                populationOccupancyProfile: PopulationOccupancyProfile.Balanced,
                plannedPeopleCount: -1));

            Assert.Equal(
                expected: InvalidProfileErrorCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "PlannedPeopleCount",
                actual: exception.PropertyName);
        }

        [Fact]
        public void Create_WithPlannedPeopleCountAboveMaximum_ThrowsDomainException()
        {
            DomainException exception = Assert.Throws<DomainException>(() => CityGenerationProfile.Create(
                sizeTier: CitySizeTier.Medium,
                urbanDensity: UrbanDensity.Balanced,
                developmentLevel: CityDevelopmentLevel.Balanced,
                economyProfile: CityEconomyProfile.Balanced,
                populationOccupancyProfile: PopulationOccupancyProfile.Balanced,
                plannedPeopleCount: CityGenerationProfile.MaxPlannedPeopleCount + 1));

            Assert.Equal(
                expected: InvalidProfileErrorCode,
                actual: exception.Code);
            Assert.Equal(
                expected: "PlannedPeopleCount",
                actual: exception.PropertyName);
        }

        [Fact]
        public void WithPlannedPeopleCount_ReturnsNewProfileWithUpdatedCount()
        {
            var profile = CityGenerationProfile.Create(
                sizeTier: CitySizeTier.Medium,
                urbanDensity: UrbanDensity.Balanced,
                developmentLevel: CityDevelopmentLevel.Balanced,
                economyProfile: CityEconomyProfile.Balanced,
                populationOccupancyProfile: PopulationOccupancyProfile.Balanced,
                plannedPeopleCount: 10_000);

            CityGenerationProfile updated = profile.WithPlannedPeopleCount(25_000);

            Assert.Equal(
                expected: 10_000,
                actual: profile.PlannedPeopleCount);
            Assert.Equal(
                expected: 25_000,
                actual: updated.PlannedPeopleCount);
            Assert.Equal(
                expected: profile.SizeTier,
                actual: updated.SizeTier);
            Assert.Equal(
                expected: profile.UrbanDensity,
                actual: updated.UrbanDensity);
            Assert.Equal(
                expected: profile.DevelopmentLevel,
                actual: updated.DevelopmentLevel);
            Assert.Equal(
                expected: profile.EconomyProfile,
                actual: updated.EconomyProfile);
            Assert.Equal(
                expected: profile.PopulationOccupancyProfile,
                actual: updated.PopulationOccupancyProfile);
        }
    }
}
