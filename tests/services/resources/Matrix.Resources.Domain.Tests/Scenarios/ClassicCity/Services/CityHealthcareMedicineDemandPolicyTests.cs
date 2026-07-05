using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Services;
using Xunit;
using static Matrix.Resources.Domain.Tests.TestSupport.ResourcesTestData;

namespace Matrix.Resources.Domain.Tests.Scenarios.ClassicCity.Services;

public sealed class CityHealthcareMedicineDemandPolicyTests
{
    private static readonly DateOnly CareDate = new(2048, 5, 6);

    [Fact]
    public void CreateDemand_ScalesByProcessedPopulationRatherThanBatchSize()
    {
        var policy = new CityHealthcareMedicineDemandPolicy();

        CityHealthcareMedicineDemandSnapshot small = policy.CreateDemand(
            processedPatientCount: 100,
            routineCareDeliveryCount: 4,
            urgentCareDeliveryCount: 3,
            acuteCareDeliveryCount: 2,
            emergencyCareDeliveryCount: 1,
            sourceRevision: 17,
            careDate: CareDate,
            observedAtUtc: CreatedAtUtc);
        CityHealthcareMedicineDemandSnapshot large = policy.CreateDemand(
            processedPatientCount: 1000,
            routineCareDeliveryCount: 40,
            urgentCareDeliveryCount: 30,
            acuteCareDeliveryCount: 20,
            emergencyCareDeliveryCount: 10,
            sourceRevision: 17,
            careDate: CareDate,
            observedAtUtc: CreatedAtUtc);

        Assert.Equal(0.0500m, small.MedicineLoadIndex);
        Assert.Equal(small.MedicineLoadIndex, large.MedicineLoadIndex);
    }

    [Fact]
    public void CreateDemand_MoreDeliveriesThanProcessedPatients_IsRejected()
    {
        var policy = new CityHealthcareMedicineDemandPolicy();

        Assert.Throws<ArgumentException>(() => policy.CreateDemand(
            processedPatientCount: 1,
            routineCareDeliveryCount: 1,
            urgentCareDeliveryCount: 1,
            acuteCareDeliveryCount: 0,
            emergencyCareDeliveryCount: 0,
            sourceRevision: 17,
            careDate: CareDate,
            observedAtUtc: CreatedAtUtc));
    }

    [Fact]
    public void ApplyConsumption_DrainsOnlyMedicineAndRefreshesRisk()
    {
        var policy = new CityHealthcareMedicineDemandPolicy();
        CityStockpileSnapshot current = CreateSnapshot();
        CityHealthcareMedicineDemandSnapshot demand = policy.CreateDemand(
            processedPatientCount: 10,
            routineCareDeliveryCount: 0,
            urgentCareDeliveryCount: 0,
            acuteCareDeliveryCount: 0,
            emergencyCareDeliveryCount: 10,
            sourceRevision: 17,
            careDate: CareDate,
            observedAtUtc: CreatedAtUtc);

        CityStockpileSnapshot updated = policy.ApplyConsumption(current, demand);

        Assert.Equal(current.Medicine.StockLevelIndex - 0.0400m, updated.Medicine.StockLevelIndex);
        Assert.True(updated.Medicine.ShortageRiskIndex > current.Medicine.ShortageRiskIndex);
        Assert.True(updated.SupplyStressIndex > current.SupplyStressIndex);
        Assert.Equal(current.Food, updated.Food);
        Assert.Equal(current.EvaluatedAtUtc, updated.EvaluatedAtUtc);
    }
}
