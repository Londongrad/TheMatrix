using Matrix.BuildingBlocks.Domain.ValueObjects;

namespace Matrix.Economy.Domain.Scenarios.ClassicCity.Models
{
    public sealed record CityBusinessPayrollSettlementOutcome(
        Money RequestedGrossPayroll,
        Money RequestedIncomeTax,
        Money PaidGrossPayroll,
        Money PaidIncomeTax,
        Money PaidNetPayroll,
        Money GrossShortfall,
        decimal FulfillmentRatio)
    {
        public bool IsFullyPaid => GrossShortfall.IsZero && PaidGrossPayroll.IsPositive;
        public bool IsPartiallyPaid => PaidGrossPayroll.IsPositive && GrossShortfall.IsPositive;
        public bool IsMissed => PaidGrossPayroll.IsZero;
    }
}
