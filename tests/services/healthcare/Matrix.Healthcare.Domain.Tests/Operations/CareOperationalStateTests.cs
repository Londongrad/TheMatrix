using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Simulation;
using Xunit;

namespace Matrix.Healthcare.Domain.Tests.Operations;

public sealed class CareOperationalStateTests
{
    private static readonly SimulationHostId HostId = new(Guid.NewGuid());
    private static readonly DateTimeOffset ObservedAtUtc =
        DateTimeOffset.Parse("2048-05-06T10:00:00+00:00");

    [Fact]
    public void OperationalProfile_CombinesQualityStockAndShortagePressure()
    {
        var profile = new CareOperationalProfile(
            new CareQualityMultiplier(1.2m),
            new CareAvailabilityIndex(0.6m),
            new CareAvailabilityIndex(0.4m));

        Assert.Equal(0.672m, profile.TreatmentEffectivenessMultiplier);
    }

    [Fact]
    public void ServiceQualityState_NewerObservation_ReplacesQuality()
    {
        CareServiceQualityState state = CareServiceQualityState.Register(
            HostId,
            new CareQualityMultiplier(0.8m),
            ObservedAtUtc);

        bool changed = state.TrySynchronize(
            new CareQualityMultiplier(1.1m),
            ObservedAtUtc.AddHours(1));

        Assert.True(changed);
        Assert.Equal(1.1m, state.QualityMultiplier.Value);
        Assert.Equal(ObservedAtUtc.AddHours(1), state.LastObservedAtUtc);
    }

    [Fact]
    public void ServiceQualityState_ConflictingDuplicate_ThrowsInvalidOperationException()
    {
        CareServiceQualityState state = CareServiceQualityState.Register(
            HostId,
            new CareQualityMultiplier(0.8m),
            ObservedAtUtc);

        Assert.Throws<InvalidOperationException>(() => state.TrySynchronize(
            new CareQualityMultiplier(0.9m),
            ObservedAtUtc));
    }

    [Fact]
    public void MedicineSupplyState_StaleRevision_DoesNotReplaceSupply()
    {
        CareMedicineSupplyState state = CareMedicineSupplyState.Register(
            HostId,
            new CareAvailabilityIndex(0.7m),
            new CareAvailabilityIndex(0.2m),
            sourceRevision: 17,
            ObservedAtUtc);

        bool changed = state.TrySynchronize(
            new CareAvailabilityIndex(0.2m),
            new CareAvailabilityIndex(0.9m),
            sourceRevision: 16,
            ObservedAtUtc.AddHours(1));

        Assert.False(changed);
        Assert.Equal(0.7m, state.StockLevel.Value);
        Assert.Equal(0.2m, state.ShortageRisk.Value);
        Assert.Equal(17, state.LastSourceRevision);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void AvailabilityIndex_OutsideUnitInterval_ThrowsArgumentOutOfRangeException(
        double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CareAvailabilityIndex((decimal)value));
    }
}
