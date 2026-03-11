using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Enums;

namespace Matrix.Economy.Domain.Entities
{
    public sealed class CityHouseholdAccountLedgerEntry
    {
        public Guid Id { get; private set; }
        public Guid HouseholdAccountId { get; private set; }
        public Guid CityId { get; private set; }
        public DateTimeOffset OccurredAtUtc { get; private set; }
        public CityHouseholdAccountLedgerEntryKind Kind { get; private set; }
        public Money Amount { get; private set; } = null!;
        public string Title { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public CityHouseholdAccountLedgerEntrySource Source { get; private set; }
        public string? ReferenceCode { get; private set; }

        private CityHouseholdAccountLedgerEntry()
        {
        }

        public CityHouseholdAccountLedgerEntry(
            Guid id,
            Guid householdAccountId,
            Guid cityId,
            DateTimeOffset occurredAtUtc,
            CityHouseholdAccountLedgerEntryKind kind,
            Money amount,
            string title,
            string? description,
            CityHouseholdAccountLedgerEntrySource source,
            string? referenceCode)
        {
            Id = GuardHelper.AgainstEmptyGuid(id, nameof(id));
            HouseholdAccountId = GuardHelper.AgainstEmptyGuid(householdAccountId, nameof(householdAccountId));
            CityId = GuardHelper.AgainstEmptyGuid(cityId, nameof(cityId));
            OccurredAtUtc = occurredAtUtc;
            Kind = kind;
            Amount = amount.IsPositive
                ? amount
                : throw new ArgumentOutOfRangeException(nameof(amount));
            Title = string.IsNullOrWhiteSpace(title)
                ? throw new ArgumentException("Title is required.", nameof(title))
                : title.Trim();
            Description = description?.Trim() ?? string.Empty;
            Source = source;
            ReferenceCode = string.IsNullOrWhiteSpace(referenceCode)
                ? null
                : referenceCode.Trim();
        }
    }
}
