using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Entities
{
    public sealed class CityPopulationEmployerFinancialStressState
    {
        private CityPopulationEmployerFinancialStressState() { }

        private CityPopulationEmployerFinancialStressState(
            CityId cityId,
            WorkplaceId workplaceId,
            decimal requestedGrossPayrollAmount,
            decimal paidGrossPayrollAmount,
            decimal missedGrossPayrollAmount,
            decimal payrollFulfillmentRatio,
            int failedPayrollCount,
            int partialPayrollCount,
            decimal currentBalanceAmount,
            decimal distressScore,
            bool hasHiringFreeze,
            bool hasLayoffPressure,
            DateTimeOffset lastEvaluatedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            ValidateAmounts(
                requestedGrossPayrollAmount: requestedGrossPayrollAmount,
                paidGrossPayrollAmount: paidGrossPayrollAmount,
                missedGrossPayrollAmount: missedGrossPayrollAmount,
                payrollFulfillmentRatio: payrollFulfillmentRatio,
                failedPayrollCount: failedPayrollCount,
                partialPayrollCount: partialPayrollCount,
                distressScore: distressScore);
            EnsureUtc(
                value: lastEvaluatedAtUtc,
                paramName: nameof(lastEvaluatedAtUtc));
            EnsureUtc(
                value: updatedAtUtc,
                paramName: nameof(updatedAtUtc));

            CityId = cityId;
            WorkplaceId = workplaceId;
            RequestedGrossPayrollAmount = decimal.Round(
                d: requestedGrossPayrollAmount,
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
            PaidGrossPayrollAmount = decimal.Round(
                d: paidGrossPayrollAmount,
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
            MissedGrossPayrollAmount = decimal.Round(
                d: missedGrossPayrollAmount,
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
            PayrollFulfillmentRatio = decimal.Round(
                d: payrollFulfillmentRatio,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
            FailedPayrollCount = failedPayrollCount;
            PartialPayrollCount = partialPayrollCount;
            CurrentBalanceAmount = decimal.Round(
                d: currentBalanceAmount,
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
            DistressScore = decimal.Round(
                d: distressScore,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
            HasHiringFreeze = hasHiringFreeze;
            HasLayoffPressure = hasLayoffPressure;
            LastEvaluatedAtUtc = lastEvaluatedAtUtc;
            UpdatedAtUtc = updatedAtUtc;
        }

        public CityId CityId { get; private set; }
        public WorkplaceId WorkplaceId { get; private set; }
        public decimal RequestedGrossPayrollAmount { get; private set; }
        public decimal PaidGrossPayrollAmount { get; private set; }
        public decimal MissedGrossPayrollAmount { get; private set; }
        public decimal PayrollFulfillmentRatio { get; private set; }
        public int FailedPayrollCount { get; private set; }
        public int PartialPayrollCount { get; private set; }
        public decimal CurrentBalanceAmount { get; private set; }
        public decimal DistressScore { get; private set; }
        public bool HasHiringFreeze { get; private set; }
        public bool HasLayoffPressure { get; private set; }
        public DateTimeOffset LastEvaluatedAtUtc { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public bool HasActiveDistress =>
            DistressScore > 0m ||
            FailedPayrollCount > 0 ||
            PartialPayrollCount > 0 ||
            MissedGrossPayrollAmount > 0m ||
            HasHiringFreeze ||
            HasLayoffPressure ||
            CurrentBalanceAmount < 0m;

        public static CityPopulationEmployerFinancialStressState Create(
            CityId cityId,
            WorkplaceId workplaceId,
            decimal requestedGrossPayrollAmount,
            decimal paidGrossPayrollAmount,
            decimal missedGrossPayrollAmount,
            decimal payrollFulfillmentRatio,
            int failedPayrollCount,
            int partialPayrollCount,
            decimal currentBalanceAmount,
            decimal distressScore,
            bool hasHiringFreeze,
            bool hasLayoffPressure,
            DateTimeOffset lastEvaluatedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            return new CityPopulationEmployerFinancialStressState(
                cityId: cityId,
                workplaceId: workplaceId,
                requestedGrossPayrollAmount: requestedGrossPayrollAmount,
                paidGrossPayrollAmount: paidGrossPayrollAmount,
                missedGrossPayrollAmount: missedGrossPayrollAmount,
                payrollFulfillmentRatio: payrollFulfillmentRatio,
                failedPayrollCount: failedPayrollCount,
                partialPayrollCount: partialPayrollCount,
                currentBalanceAmount: currentBalanceAmount,
                distressScore: distressScore,
                hasHiringFreeze: hasHiringFreeze,
                hasLayoffPressure: hasLayoffPressure,
                lastEvaluatedAtUtc: lastEvaluatedAtUtc,
                updatedAtUtc: updatedAtUtc);
        }

        public void ApplySnapshot(
            decimal requestedGrossPayrollAmount,
            decimal paidGrossPayrollAmount,
            decimal missedGrossPayrollAmount,
            decimal payrollFulfillmentRatio,
            int failedPayrollCount,
            int partialPayrollCount,
            decimal currentBalanceAmount,
            decimal distressScore,
            bool hasHiringFreeze,
            bool hasLayoffPressure,
            DateTimeOffset lastEvaluatedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            ValidateAmounts(
                requestedGrossPayrollAmount: requestedGrossPayrollAmount,
                paidGrossPayrollAmount: paidGrossPayrollAmount,
                missedGrossPayrollAmount: missedGrossPayrollAmount,
                payrollFulfillmentRatio: payrollFulfillmentRatio,
                failedPayrollCount: failedPayrollCount,
                partialPayrollCount: partialPayrollCount,
                distressScore: distressScore);
            EnsureUtc(
                value: lastEvaluatedAtUtc,
                paramName: nameof(lastEvaluatedAtUtc));
            EnsureUtc(
                value: updatedAtUtc,
                paramName: nameof(updatedAtUtc));

            RequestedGrossPayrollAmount = decimal.Round(
                d: requestedGrossPayrollAmount,
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
            PaidGrossPayrollAmount = decimal.Round(
                d: paidGrossPayrollAmount,
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
            MissedGrossPayrollAmount = decimal.Round(
                d: missedGrossPayrollAmount,
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
            PayrollFulfillmentRatio = decimal.Round(
                d: payrollFulfillmentRatio,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
            FailedPayrollCount = failedPayrollCount;
            PartialPayrollCount = partialPayrollCount;
            CurrentBalanceAmount = decimal.Round(
                d: currentBalanceAmount,
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
            DistressScore = decimal.Round(
                d: distressScore,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
            HasHiringFreeze = hasHiringFreeze;
            HasLayoffPressure = hasLayoffPressure;
            LastEvaluatedAtUtc = lastEvaluatedAtUtc;
            UpdatedAtUtc = updatedAtUtc;
        }

        private static void ValidateAmounts(
            decimal requestedGrossPayrollAmount,
            decimal paidGrossPayrollAmount,
            decimal missedGrossPayrollAmount,
            decimal payrollFulfillmentRatio,
            int failedPayrollCount,
            int partialPayrollCount,
            decimal distressScore)
        {
            if (requestedGrossPayrollAmount < 0m)
                throw new ArgumentOutOfRangeException(nameof(requestedGrossPayrollAmount));

            if (paidGrossPayrollAmount < 0m)
                throw new ArgumentOutOfRangeException(nameof(paidGrossPayrollAmount));

            if (missedGrossPayrollAmount < 0m)
                throw new ArgumentOutOfRangeException(nameof(missedGrossPayrollAmount));

            if (payrollFulfillmentRatio is < 0m or > 1m)
                throw new ArgumentOutOfRangeException(nameof(payrollFulfillmentRatio));

            if (failedPayrollCount < 0)
                throw new ArgumentOutOfRangeException(nameof(failedPayrollCount));

            if (partialPayrollCount < 0)
                throw new ArgumentOutOfRangeException(nameof(partialPayrollCount));

            if (paidGrossPayrollAmount > requestedGrossPayrollAmount)
                throw new ArgumentOutOfRangeException(nameof(paidGrossPayrollAmount));

            if (decimal.Round(
                    d: paidGrossPayrollAmount + missedGrossPayrollAmount,
                    decimals: 2,
                    mode: MidpointRounding.AwayFromZero) >
                requestedGrossPayrollAmount + 0.01m)
                throw new ArgumentOutOfRangeException(nameof(missedGrossPayrollAmount));

            if (distressScore is < 0m or > 1m)
                throw new ArgumentOutOfRangeException(nameof(distressScore));
        }

        private static void EnsureUtc(
            DateTimeOffset value,
            string paramName)
        {
            GuardHelper.Ensure(
                condition: value.Offset == TimeSpan.Zero,
                value: value,
                errorFactory: DomainErrorsFactory.TimestampMustBeUtc,
                propertyName: paramName);
        }
    }
}
