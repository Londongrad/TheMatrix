namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.ApplyCityHealthcareMedicineDemand;

public sealed record ApplyCityHealthcareMedicineDemandResult(
    ApplyCityHealthcareMedicineDemandStatus Status,
    decimal MedicineLoadIndex,
    decimal MedicineStockLevelIndex,
    decimal MedicineShortageRiskIndex,
    long SourceRevision);
