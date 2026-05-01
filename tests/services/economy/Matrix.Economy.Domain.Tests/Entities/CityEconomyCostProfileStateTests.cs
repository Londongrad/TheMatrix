using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Entities;

public sealed class CityEconomyCostProfileStateTests
{
    [Fact]
    public void Create_WhenSeedIsValid_InitializesBaseAndCurrentValues()
    {
        Guid cityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        CityEconomyCostProfileSnapshot seed = CreateSnapshot(
            wageMultiplier: 1.12345m,
            retailPriceMultiplier: 1.23456m,
            housingCostMultiplier: 1.34567m,
            utilityCostMultiplier: 1.45678m,
            costOfLivingIndex: 1.11119m,
            affordabilityIndex: 0.88881m,
            evaluatedAtUtc: new DateTimeOffset(2048, 2, 3, 4, 5, 6, TimeSpan.Zero));
        DateTimeOffset updatedAtUtc = new(2048, 2, 3, 5, 0, 0, TimeSpan.Zero);

        CityEconomyCostProfileState state = CityEconomyCostProfileState.Create(
            cityId: cityId,
            seed: seed,
            updatedAtUtc: updatedAtUtc);

        Assert.Equal(cityId, state.CityId);
        Assert.Equal(1.1235m, state.BaseWageMultiplier);
        Assert.Equal(1.2346m, state.BaseRetailPriceMultiplier);
        Assert.Equal(1.3457m, state.BaseHousingCostMultiplier);
        Assert.Equal(1.4568m, state.BaseUtilityCostMultiplier);
        Assert.Equal(1.1235m, state.WageMultiplier);
        Assert.Equal(1.2346m, state.RetailPriceMultiplier);
        Assert.Equal(1.3457m, state.HousingCostMultiplier);
        Assert.Equal(1.4568m, state.UtilityCostMultiplier);
        Assert.Equal(1.1112m, state.CostOfLivingIndex);
        Assert.Equal(0.8888m, state.AffordabilityIndex);
        Assert.Equal(seed.EvaluatedAtUtc, state.LastEvaluatedAtUtc);
        Assert.Equal(updatedAtUtc, state.UpdatedAtUtc);
    }

    [Fact]
    public void Create_WhenCityIdIsEmpty_ThrowsDomainException()
    {
        DomainException exception = Assert.Throws<DomainException>(
            () => CityEconomyCostProfileState.Create(
                cityId: Guid.Empty,
                seed: CreateSnapshot(),
                updatedAtUtc: new DateTimeOffset(2048, 2, 3, 5, 0, 0, TimeSpan.Zero)));

        Assert.Equal("Domain.Guard.EmptyGuid", exception.Code);
    }

    [Fact]
    public void Create_WhenSeedHasOutOfRangeMultiplier_ThrowsArgumentOutOfRangeException()
    {
        CityEconomyCostProfileSnapshot seed = CreateSnapshot(wageMultiplier: 0.39m);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => CityEconomyCostProfileState.Create(
                cityId: Guid.NewGuid(),
                seed: seed,
                updatedAtUtc: new DateTimeOffset(2048, 2, 3, 5, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void ApplySnapshot_WhenSnapshotIsValid_UpdatesCurrentValuesOnly()
    {
        CityEconomyCostProfileState state = CityEconomyCostProfileState.Create(
            cityId: Guid.NewGuid(),
            seed: CreateSnapshot(
                wageMultiplier: 1.1m,
                retailPriceMultiplier: 1.2m,
                housingCostMultiplier: 1.3m,
                utilityCostMultiplier: 1.4m,
                costOfLivingIndex: 1.5m,
                affordabilityIndex: 0.9m,
                evaluatedAtUtc: new DateTimeOffset(2048, 2, 3, 4, 0, 0, TimeSpan.Zero)),
            updatedAtUtc: new DateTimeOffset(2048, 2, 3, 4, 30, 0, TimeSpan.Zero));
        CityEconomyCostProfileSnapshot snapshot = CreateSnapshot(
            wageMultiplier: 1.23451m,
            retailPriceMultiplier: 1.11111m,
            housingCostMultiplier: 1.22222m,
            utilityCostMultiplier: 1.33333m,
            costOfLivingIndex: 1.44444m,
            affordabilityIndex: 0.77777m,
            evaluatedAtUtc: new DateTimeOffset(2048, 2, 4, 1, 0, 0, TimeSpan.Zero));
        DateTimeOffset updatedAtUtc = new(2048, 2, 4, 2, 0, 0, TimeSpan.Zero);

        state.ApplySnapshot(snapshot, updatedAtUtc);

        Assert.Equal(1.1m, state.BaseWageMultiplier);
        Assert.Equal(1.2345m, state.WageMultiplier);
        Assert.Equal(1.1111m, state.RetailPriceMultiplier);
        Assert.Equal(1.2222m, state.HousingCostMultiplier);
        Assert.Equal(1.3333m, state.UtilityCostMultiplier);
        Assert.Equal(1.4444m, state.CostOfLivingIndex);
        Assert.Equal(0.7778m, state.AffordabilityIndex);
        Assert.Equal(snapshot.EvaluatedAtUtc, state.LastEvaluatedAtUtc);
        Assert.Equal(updatedAtUtc, state.UpdatedAtUtc);
    }

    [Fact]
    public void ApplySnapshot_WhenTimestampIsNotUtc_ThrowsArgumentException()
    {
        CityEconomyCostProfileState state = CityEconomyCostProfileState.Create(
            cityId: Guid.NewGuid(),
            seed: CreateSnapshot(),
            updatedAtUtc: new DateTimeOffset(2048, 2, 3, 5, 0, 0, TimeSpan.Zero));
        CityEconomyCostProfileSnapshot snapshot = CreateSnapshot(
            evaluatedAtUtc: new DateTimeOffset(2048, 2, 4, 1, 0, 0, TimeSpan.FromHours(3)));

        Assert.Throws<ArgumentException>(
            () => state.ApplySnapshot(
                snapshot: snapshot,
                updatedAtUtc: new DateTimeOffset(2048, 2, 4, 2, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void ToSnapshot_WhenCalled_ReturnsCurrentValues()
    {
        CityEconomyCostProfileState state = CityEconomyCostProfileState.Create(
            cityId: Guid.NewGuid(),
            seed: CreateSnapshot(),
            updatedAtUtc: new DateTimeOffset(2048, 2, 3, 5, 0, 0, TimeSpan.Zero));
        CityEconomyCostProfileSnapshot snapshot = CreateSnapshot(
            wageMultiplier: 1.3m,
            retailPriceMultiplier: 1.4m,
            housingCostMultiplier: 1.5m,
            utilityCostMultiplier: 1.6m,
            costOfLivingIndex: 1.7m,
            affordabilityIndex: 0.8m,
            evaluatedAtUtc: new DateTimeOffset(2048, 2, 4, 1, 0, 0, TimeSpan.Zero));
        state.ApplySnapshot(snapshot, new DateTimeOffset(2048, 2, 4, 2, 0, 0, TimeSpan.Zero));

        CityEconomyCostProfileSnapshot result = state.ToSnapshot();

        Assert.Equal(1.3m, result.WageMultiplier);
        Assert.Equal(1.4m, result.RetailPriceMultiplier);
        Assert.Equal(1.5m, result.HousingCostMultiplier);
        Assert.Equal(1.6m, result.UtilityCostMultiplier);
        Assert.Equal(1.7m, result.CostOfLivingIndex);
        Assert.Equal(0.8m, result.AffordabilityIndex);
        Assert.Equal(snapshot.EvaluatedAtUtc, result.EvaluatedAtUtc);
    }

    private static CityEconomyCostProfileSnapshot CreateSnapshot(
        decimal wageMultiplier = 1.0m,
        decimal retailPriceMultiplier = 1.0m,
        decimal housingCostMultiplier = 1.0m,
        decimal utilityCostMultiplier = 1.0m,
        decimal costOfLivingIndex = 1.0m,
        decimal affordabilityIndex = 1.0m,
        DateTimeOffset? evaluatedAtUtc = null)
    {
        return new CityEconomyCostProfileSnapshot(
            WageMultiplier: wageMultiplier,
            RetailPriceMultiplier: retailPriceMultiplier,
            HousingCostMultiplier: housingCostMultiplier,
            UtilityCostMultiplier: utilityCostMultiplier,
            CostOfLivingIndex: costOfLivingIndex,
            AffordabilityIndex: affordabilityIndex,
            EvaluatedAtUtc: evaluatedAtUtc ?? new DateTimeOffset(2048, 2, 3, 4, 5, 6, TimeSpan.Zero));
    }
}
