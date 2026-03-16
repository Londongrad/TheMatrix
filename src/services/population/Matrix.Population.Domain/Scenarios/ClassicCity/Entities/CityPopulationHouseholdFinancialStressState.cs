using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Entities
{
    public sealed class CityPopulationHouseholdFinancialStressState
    {
        private CityPopulationHouseholdFinancialStressState() { }

        private CityPopulationHouseholdFinancialStressState(
            CityId cityId,
            HouseholdId householdId,
            int overdueObligationCount,
            int overdueRentCount,
            int overdueUtilityCount,
            decimal totalOverdueAmount,
            decimal distressScore,
            DateTimeOffset lastEvaluatedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            ValidateCounts(
                overdueObligationCount: overdueObligationCount,
                overdueRentCount: overdueRentCount,
                overdueUtilityCount: overdueUtilityCount);
            ValidateAmounts(
                totalOverdueAmount: totalOverdueAmount,
                distressScore: distressScore);
            EnsureUtc(
                value: lastEvaluatedAtUtc,
                paramName: nameof(lastEvaluatedAtUtc));
            EnsureUtc(
                value: updatedAtUtc,
                paramName: nameof(updatedAtUtc));

            CityId = cityId;
            HouseholdId = householdId;
            OverdueObligationCount = overdueObligationCount;
            OverdueRentCount = overdueRentCount;
            OverdueUtilityCount = overdueUtilityCount;
            TotalOverdueAmount = decimal.Round(
                d: totalOverdueAmount,
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
            DistressScore = decimal.Round(
                d: distressScore,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
            LastEvaluatedAtUtc = lastEvaluatedAtUtc;
            UpdatedAtUtc = updatedAtUtc;
        }

        public CityId CityId { get; private set; }
        public HouseholdId HouseholdId { get; private set; }
        public int OverdueObligationCount { get; private set; }
        public int OverdueRentCount { get; private set; }
        public int OverdueUtilityCount { get; private set; }
        public decimal TotalOverdueAmount { get; private set; }
        public decimal DistressScore { get; private set; }
        public DateTimeOffset LastEvaluatedAtUtc { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public bool HasActiveDistress =>
            OverdueObligationCount > 0 ||
            TotalOverdueAmount > 0m ||
            DistressScore > 0m;

        public static CityPopulationHouseholdFinancialStressState Create(
            CityId cityId,
            HouseholdId householdId,
            int overdueObligationCount,
            int overdueRentCount,
            int overdueUtilityCount,
            decimal totalOverdueAmount,
            decimal distressScore,
            DateTimeOffset lastEvaluatedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            return new CityPopulationHouseholdFinancialStressState(
                cityId: cityId,
                householdId: householdId,
                overdueObligationCount: overdueObligationCount,
                overdueRentCount: overdueRentCount,
                overdueUtilityCount: overdueUtilityCount,
                totalOverdueAmount: totalOverdueAmount,
                distressScore: distressScore,
                lastEvaluatedAtUtc: lastEvaluatedAtUtc,
                updatedAtUtc: updatedAtUtc);
        }

        public void ApplySnapshot(
            int overdueObligationCount,
            int overdueRentCount,
            int overdueUtilityCount,
            decimal totalOverdueAmount,
            decimal distressScore,
            DateTimeOffset lastEvaluatedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            ValidateCounts(
                overdueObligationCount: overdueObligationCount,
                overdueRentCount: overdueRentCount,
                overdueUtilityCount: overdueUtilityCount);
            ValidateAmounts(
                totalOverdueAmount: totalOverdueAmount,
                distressScore: distressScore);
            EnsureUtc(
                value: lastEvaluatedAtUtc,
                paramName: nameof(lastEvaluatedAtUtc));
            EnsureUtc(
                value: updatedAtUtc,
                paramName: nameof(updatedAtUtc));

            OverdueObligationCount = overdueObligationCount;
            OverdueRentCount = overdueRentCount;
            OverdueUtilityCount = overdueUtilityCount;
            TotalOverdueAmount = decimal.Round(
                d: totalOverdueAmount,
                decimals: 2,
                mode: MidpointRounding.AwayFromZero);
            DistressScore = decimal.Round(
                d: distressScore,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
            LastEvaluatedAtUtc = lastEvaluatedAtUtc;
            UpdatedAtUtc = updatedAtUtc;
        }

        private static void ValidateCounts(
            int overdueObligationCount,
            int overdueRentCount,
            int overdueUtilityCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value: overdueObligationCount);
            ArgumentOutOfRangeException.ThrowIfNegative(value: overdueRentCount);
            ArgumentOutOfRangeException.ThrowIfNegative(value: overdueUtilityCount);
        }

        private static void ValidateAmounts(
            decimal totalOverdueAmount,
            decimal distressScore)
        {
            if (totalOverdueAmount < 0m)
                throw new ArgumentOutOfRangeException(nameof(totalOverdueAmount));

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
