using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.ValueObjects;

namespace Matrix.Economy.Domain.Entities
{
    public sealed class CityBudgetSettlement
    {
        public Guid Id { get; private set; }
        public Guid CityId { get; private set; }
        public long TickId { get; private set; }
        public DateOnly CurrentDate { get; private set; }
        public int SettledDays { get; private set; }
        public int HouseholdCount { get; private set; }
        public int ResidentCount { get; private set; }
        public Money GrossPayroll { get; private set; } = null!;
        public Money IncomeTax { get; private set; } = null!;
        public Money NetPayroll { get; private set; } = null!;
        public Money RetailTurnover { get; private set; } = null!;
        public Money RetailTax { get; private set; } = null!;
        public Money HousingSpend { get; private set; } = null!;
        public string CorrelationId { get; private set; } = string.Empty;
        public DateTimeOffset OccurredAtUtc { get; private set; }

        private CityBudgetSettlement()
        {
        }

        public CityBudgetSettlement(
            Guid id,
            Guid cityId,
            long tickId,
            DateOnly currentDate,
            int settledDays,
            int householdCount,
            int residentCount,
            Money grossPayroll,
            Money incomeTax,
            Money netPayroll,
            Money retailTurnover,
            Money retailTax,
            Money housingSpend,
            string correlationId,
            DateTimeOffset occurredAtUtc)
        {
            Id = GuardHelper.AgainstEmptyGuid(id, nameof(id));
            CityId = GuardHelper.AgainstEmptyGuid(cityId, nameof(cityId));
            TickId = tickId > 0 ? tickId : throw new ArgumentOutOfRangeException(nameof(tickId));
            CurrentDate = currentDate;
            SettledDays = settledDays > 0 ? settledDays : throw new ArgumentOutOfRangeException(nameof(settledDays));
            HouseholdCount = householdCount >= 0 ? householdCount : throw new ArgumentOutOfRangeException(nameof(householdCount));
            ResidentCount = residentCount >= 0 ? residentCount : throw new ArgumentOutOfRangeException(nameof(residentCount));
            GrossPayroll = grossPayroll;
            IncomeTax = incomeTax;
            NetPayroll = netPayroll;
            RetailTurnover = retailTurnover;
            RetailTax = retailTax;
            HousingSpend = housingSpend;
            CorrelationId = string.IsNullOrWhiteSpace(correlationId)
                ? throw new ArgumentException("Correlation id is required.", nameof(correlationId))
                : correlationId.Trim();
            OccurredAtUtc = occurredAtUtc;
        }
    }
}
