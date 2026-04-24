using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Scenarios.ClassicCity.Cities;

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

        Assert.Equal(CitySizeTier.Medium, profile.SizeTier);
        Assert.Equal(UrbanDensity.Balanced, profile.UrbanDensity);
        Assert.Equal(CityDevelopmentLevel.Balanced, profile.DevelopmentLevel);
        Assert.Equal(CityEconomyProfile.Balanced, profile.EconomyProfile);
        Assert.Equal(PopulationOccupancyProfile.Balanced, profile.PopulationOccupancyProfile);
        Assert.Equal(42_000, profile.PlannedPeopleCount);
    }

    [Fact]
    public void Create_WithInvalidSizeTier_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => CityGenerationProfile.Create(
            sizeTier: (CitySizeTier)999,
            urbanDensity: UrbanDensity.Balanced,
            developmentLevel: CityDevelopmentLevel.Balanced,
            economyProfile: CityEconomyProfile.Balanced,
            populationOccupancyProfile: PopulationOccupancyProfile.Balanced));

        Assert.Equal(InvalidEnumErrorCode, exception.Code);
        Assert.Equal("SizeTier", exception.PropertyName);
    }

    [Fact]
    public void Create_WithNegativePlannedPeopleCount_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => CityGenerationProfile.Create(
            sizeTier: CitySizeTier.Medium,
            urbanDensity: UrbanDensity.Balanced,
            developmentLevel: CityDevelopmentLevel.Balanced,
            economyProfile: CityEconomyProfile.Balanced,
            populationOccupancyProfile: PopulationOccupancyProfile.Balanced,
            plannedPeopleCount: -1));

        Assert.Equal(InvalidProfileErrorCode, exception.Code);
        Assert.Equal("PlannedPeopleCount", exception.PropertyName);
    }

    [Fact]
    public void Create_WithPlannedPeopleCountAboveMaximum_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => CityGenerationProfile.Create(
            sizeTier: CitySizeTier.Medium,
            urbanDensity: UrbanDensity.Balanced,
            developmentLevel: CityDevelopmentLevel.Balanced,
            economyProfile: CityEconomyProfile.Balanced,
            populationOccupancyProfile: PopulationOccupancyProfile.Balanced,
            plannedPeopleCount: CityGenerationProfile.MaxPlannedPeopleCount + 1));

        Assert.Equal(InvalidProfileErrorCode, exception.Code);
        Assert.Equal("PlannedPeopleCount", exception.PropertyName);
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

        var updated = profile.WithPlannedPeopleCount(25_000);

        Assert.Equal(10_000, profile.PlannedPeopleCount);
        Assert.Equal(25_000, updated.PlannedPeopleCount);
        Assert.Equal(profile.SizeTier, updated.SizeTier);
        Assert.Equal(profile.UrbanDensity, updated.UrbanDensity);
        Assert.Equal(profile.DevelopmentLevel, updated.DevelopmentLevel);
        Assert.Equal(profile.EconomyProfile, updated.EconomyProfile);
        Assert.Equal(profile.PopulationOccupancyProfile, updated.PopulationOccupancyProfile);
    }
}
