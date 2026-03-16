using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Enums;

namespace Matrix.Economy.Domain.Entities
{
    public sealed class CityBusinessLedgerEntry
    {
        private CityBusinessLedgerEntry() { }

        public CityBusinessLedgerEntry(
            Guid id,
            Guid businessId,
            Guid cityId,
            DateTimeOffset occurredAtUtc,
            CityBusinessLedgerEntryKind kind,
            Money amount,
            Money taxAmount,
            string title,
            string? description,
            CityBusinessLedgerEntrySource source,
            string? referenceCode)
        {
            Id = GuardHelper.AgainstEmptyGuid(
                id: id,
                propertyName: nameof(id));
            BusinessId = GuardHelper.AgainstEmptyGuid(
                id: businessId,
                propertyName: nameof(businessId));
            CityId = GuardHelper.AgainstEmptyGuid(
                id: cityId,
                propertyName: nameof(cityId));
            OccurredAtUtc = occurredAtUtc;
            Kind = kind;
            Amount = amount.IsPositive
                ? amount
                : throw new ArgumentOutOfRangeException(nameof(amount));
            TaxAmount = taxAmount.IsNegative
                ? throw new ArgumentOutOfRangeException(nameof(taxAmount))
                : taxAmount;
            Title = string.IsNullOrWhiteSpace(title)
                ? throw new ArgumentException(
                    message: "Title is required.",
                    paramName: nameof(title))
                : title.Trim();
            Description = description?.Trim() ?? string.Empty;
            Source = source;
            ReferenceCode = string.IsNullOrWhiteSpace(referenceCode)
                ? null
                : referenceCode.Trim();
        }

        public Guid Id { get; private set; }
        public Guid BusinessId { get; private set; }
        public Guid CityId { get; private set; }
        public DateTimeOffset OccurredAtUtc { get; private set; }
        public CityBusinessLedgerEntryKind Kind { get; private set; }
        public Money Amount { get; private set; } = null!;
        public Money TaxAmount { get; private set; } = null!;
        public string Title { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public CityBusinessLedgerEntrySource Source { get; private set; }
        public string? ReferenceCode { get; private set; }
    }
}
