using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityPopulationClimateAdaptationPolicyTests
{
    [Fact]
    public void GetToleranceScore_WhenEnvironmentIsNull_ReturnsZero()
    {
        var policy = new CityPopulationClimateAdaptationPolicy();

        int score = policy.GetToleranceScore(
            environment: null,
            currentDate: new DateOnly(2048, 7, 10),
            weatherType: PopulationWeatherType.Heatwave);

        Assert.Equal(0, score);
    }

    [Fact]
    public void GetToleranceScore_WhenSouthernHemisphereSummerHeatwave_AdjustsForSeason()
    {
        var policy = new CityPopulationClimateAdaptationPolicy();

        int score = policy.GetToleranceScore(
            environment: CreateEnvironment(
                climateZone: PopulationClimateZone.Tropical,
                hemisphere: PopulationHemisphere.Southern),
            currentDate: new DateOnly(2048, 1, 10),
            weatherType: PopulationWeatherType.Heatwave);

        Assert.Equal(2, score);
    }

    [Fact]
    public void AdjustNegativeDelta_WhenToleranceIsPositive_ReducesMagnitudeWithoutCrossingAboveZero()
    {
        var policy = new CityPopulationClimateAdaptationPolicy();

        int softened = policy.AdjustNegativeDelta(delta: -4, toleranceScore: 2);
        int clamped = policy.AdjustNegativeDelta(delta: -1, toleranceScore: 2);

        Assert.Equal(-2, softened);
        Assert.Equal(0, clamped);
    }

    [Fact]
    public void AdjustPositiveReliefDelta_WhenToleranceIsNegative_IncreasesRelief()
    {
        var policy = new CityPopulationClimateAdaptationPolicy();

        int increased = policy.AdjustPositiveReliefDelta(delta: 3, toleranceScore: -2);
        int dampened = policy.AdjustPositiveReliefDelta(delta: 3, toleranceScore: 1);

        Assert.Equal(5, increased);
        Assert.Equal(2, dampened);
    }

    [Fact]
    public void AdjustExposureStepHours_WhenToleranceChanges_UsesFactorAndClamp()
    {
        var policy = new CityPopulationClimateAdaptationPolicy();

        decimal shortened = policy.AdjustExposureStepHours(stepHours: 3m, toleranceScore: 2);
        decimal clamped = policy.AdjustExposureStepHours(stepHours: 120m, toleranceScore: -2);

        Assert.Equal(4.50m, shortened);
        Assert.Equal(72m, clamped);
    }

    private static CityPopulationEnvironment CreateEnvironment(
        PopulationClimateZone climateZone,
        PopulationHemisphere hemisphere)
    {
        return CityPopulationEnvironment.Create(
            cityId: CityId.From(Guid.Parse("01010101-0202-0303-0404-050505050505")),
            climateZone: climateZone,
            hemisphere: hemisphere,
            utcOffsetMinutes: 0,
            createdAtUtc: new DateTimeOffset(2048, 1, 10, 0, 0, 0, TimeSpan.Zero));
    }
}
