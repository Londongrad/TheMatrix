using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Resources.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.Resources.Domain.Scenarios.ClassicCity.Services;

public sealed class CityHealthcareMedicineDemandPolicy
{
    private const decimal MaximumDailyMedicineStockDrain = 0.0400m;

    public CityHealthcareMedicineDemandSnapshot CreateDemand(
        int processedPatientCount,
        int routineCareDeliveryCount,
        int urgentCareDeliveryCount,
        int acuteCareDeliveryCount,
        int emergencyCareDeliveryCount,
        long sourceRevision,
        DateOnly careDate,
        DateTimeOffset observedAtUtc)
    {
        int processed = EnsureCount(processedPatientCount, nameof(processedPatientCount));
        int routine = EnsureCount(routineCareDeliveryCount, nameof(routineCareDeliveryCount));
        int urgent = EnsureCount(urgentCareDeliveryCount, nameof(urgentCareDeliveryCount));
        int acute = EnsureCount(acuteCareDeliveryCount, nameof(acuteCareDeliveryCount));
        int emergency = EnsureCount(emergencyCareDeliveryCount, nameof(emergencyCareDeliveryCount));
        int delivered = checked(routine + urgent + acute + emergency);
        if (delivered > processed)
            throw new ArgumentException(
                "Delivered care count cannot exceed the processed patient count.");
        if (sourceRevision < 0)
            throw new ArgumentOutOfRangeException(nameof(sourceRevision));
        if (observedAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException(
                "Healthcare demand observation timestamps must be expressed in UTC.",
                nameof(observedAtUtc));

        decimal weightedCare =
            (routine * 0.25m) +
            (urgent * 0.50m) +
            (acute * 0.75m) +
            emergency;
        decimal medicineLoad = processed == 0
            ? 0m
            : ClampIndex(weightedCare / processed);

        return new CityHealthcareMedicineDemandSnapshot(
            ProcessedPatientCount: processed,
            RoutineCareDeliveryCount: routine,
            UrgentCareDeliveryCount: urgent,
            AcuteCareDeliveryCount: acute,
            EmergencyCareDeliveryCount: emergency,
            MedicineLoadIndex: medicineLoad,
            SourceRevision: sourceRevision,
            CareDate: careDate,
            ObservedAtUtc: observedAtUtc);
    }

    public CityStockpileSnapshot ApplyConsumption(
        CityStockpileSnapshot current,
        CityHealthcareMedicineDemandSnapshot demand)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(demand);

        decimal stockLevel = ClampIndex(
            current.Medicine.StockLevelIndex -
            (demand.MedicineLoadIndex * MaximumDailyMedicineStockDrain));
        decimal shortageRisk = CalculateShortageRisk(
            stockLevel,
            current.Medicine.DemandPressureIndex,
            current.Medicine.ResupplyReadinessIndex,
            current.EmergencyRationingEnabled);
        var medicine = new CityStockpileLineSnapshot(
            Kind: CityResourceKind.Medicine,
            StockLevelIndex: stockLevel,
            DemandPressureIndex: current.Medicine.DemandPressureIndex,
            ResupplyReadinessIndex: current.Medicine.ResupplyReadinessIndex,
            ShortageRiskIndex: shortageRisk);

        decimal supplyStress = ClampIndex(
            current.SupplyStressIndex +
            ((medicine.ShortageRiskIndex - current.Medicine.ShortageRiskIndex) * 0.16m));

        return current with
        {
            Medicine = medicine,
            SupplyStressIndex = supplyStress
        };
    }

    private static decimal CalculateShortageRisk(
        decimal stockLevelIndex,
        decimal demandPressureIndex,
        decimal resupplyReadinessIndex,
        bool emergencyRationingEnabled)
    {
        decimal rationingRelief = emergencyRationingEnabled ? 0.07m : 0m;
        return ClampIndex(
            0.14m +
            ((1m - stockLevelIndex) * 0.42m) +
            (demandPressureIndex * 0.22m) +
            ((1m - resupplyReadinessIndex) * 0.16m) +
            (0.76m * 0.12m) -
            rationingRelief);
    }

    private static int EnsureCount(int value, string paramName)
    {
        return value >= 0
            ? value
            : throw new ArgumentOutOfRangeException(paramName);
    }

    private static decimal ClampIndex(decimal value)
    {
        return decimal.Round(
            Math.Min(1m, Math.Max(0m, value)),
            decimals: 4,
            mode: MidpointRounding.AwayFromZero);
    }
}
