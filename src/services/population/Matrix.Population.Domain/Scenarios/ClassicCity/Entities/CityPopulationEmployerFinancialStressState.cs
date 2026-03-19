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
            decimal recentGrossPayrollAmount,
            decimal currentBalanceAmount,
            decimal distressScore,
            bool hasHiringFreeze,
            bool hasLayoffPressure,
            DateTimeOffset lastEvaluatedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            ValidateAmounts(
                recentGrossPayrollAmount: recentGrossPayrollAmount,
                distressScore: distressScore);
            EnsureUtc(
                value: lastEvaluatedAtUtc,
                paramName: nameof(lastEvaluatedAtUtc));
            EnsureUtc(
                value: updatedAtUtc,
                paramName: nameof(updatedAtUtc));

            CityId = cityId;
            WorkplaceId = workplaceId;
            RecentGrossPayrollAmount = decimal.Round(
                d: recentGrossPayrollAmount,
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
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
        public decimal RecentGrossPayrollAmount { get; private set; }
        public decimal CurrentBalanceAmount { get; private set; }
        public decimal DistressScore { get; private set; }
        public bool HasHiringFreeze { get; private set; }
        public bool HasLayoffPressure { get; private set; }
        public DateTimeOffset LastEvaluatedAtUtc { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public bool HasActiveDistress =>
            DistressScore > 0m ||
            HasHiringFreeze ||
            HasLayoffPressure ||
            CurrentBalanceAmount < 0m;

        public static CityPopulationEmployerFinancialStressState Create(
            CityId cityId,
            WorkplaceId workplaceId,
            decimal recentGrossPayrollAmount,
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
                recentGrossPayrollAmount: recentGrossPayrollAmount,
                currentBalanceAmount: currentBalanceAmount,
                distressScore: distressScore,
                hasHiringFreeze: hasHiringFreeze,
                hasLayoffPressure: hasLayoffPressure,
                lastEvaluatedAtUtc: lastEvaluatedAtUtc,
                updatedAtUtc: updatedAtUtc);
        }

        public void ApplySnapshot(
            decimal recentGrossPayrollAmount,
            decimal currentBalanceAmount,
            decimal distressScore,
            bool hasHiringFreeze,
            bool hasLayoffPressure,
            DateTimeOffset lastEvaluatedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            ValidateAmounts(
                recentGrossPayrollAmount: recentGrossPayrollAmount,
                distressScore: distressScore);
            EnsureUtc(
                value: lastEvaluatedAtUtc,
                paramName: nameof(lastEvaluatedAtUtc));
            EnsureUtc(
                value: updatedAtUtc,
                paramName: nameof(updatedAtUtc));

            RecentGrossPayrollAmount = decimal.Round(
                d: recentGrossPayrollAmount,
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
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
            decimal recentGrossPayrollAmount,
            decimal distressScore)
        {
            if (recentGrossPayrollAmount < 0m)
                throw new ArgumentOutOfRangeException(nameof(recentGrossPayrollAmount));

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
