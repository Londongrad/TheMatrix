using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.Economy.Domain.Scenarios.ClassicCity.Entities
{
    public sealed class CityBudgetLedgerEntry
    {
        private CityBudgetLedgerEntry() { }

        public CityBudgetLedgerEntry(
            Guid id,
            Guid cityId,
            DateTimeOffset occurredAtUtc,
            CityBudgetLedgerEntryKind kind,
            CityBudgetCategory category,
            Money amount,
            string title,
            string? description,
            CityBudgetLedgerEntrySource source,
            string? referenceCode)
        {
            Id = GuardHelper.AgainstEmptyGuid(
                id: id,
                propertyName: nameof(id));
            CityId = GuardHelper.AgainstEmptyGuid(
                id: cityId,
                propertyName: nameof(cityId));
            OccurredAtUtc = occurredAtUtc;
            Kind = kind;
            Category = category;
            Amount = amount.Amount > 0m
                ? amount
                : throw new ArgumentOutOfRangeException(nameof(amount));
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
        public Guid CityId { get; private set; }
        public DateTimeOffset OccurredAtUtc { get; private set; }
        public CityBudgetLedgerEntryKind Kind { get; private set; }
        public CityBudgetCategory Category { get; private set; }
        public Money Amount { get; private set; } = null!;
        public string Title { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public CityBudgetLedgerEntrySource Source { get; private set; }
        public string? ReferenceCode { get; private set; }
    }
}
