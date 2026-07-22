namespace Matrix.Education.Contracts.Events;

public sealed record EducationAgeIncomeBandV1(int MinimumAge, decimal DailyIncome);

public sealed record EducationEconomicEffectsV1(
    IReadOnlyList<EducationAgeIncomeBandV1> TransferIncome,
    decimal EmploymentIncomeBonus,
    double EmploymentOpportunityBonus,
    double EmploymentAvailabilityFactor,
    decimal RetailStoreSpendShareAdjustment,
    decimal ServiceSpendShareAdjustment,
    decimal MunicipalSpendShareAdjustment);
