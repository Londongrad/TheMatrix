using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Entities
{
    public sealed class Household
    {
        private Household() { }

        private Household(
            HouseholdId id,
            HouseholdSize size,
            DateTimeOffset createdAtUtc,
            Money cashReserve)
        {
            EnsureUtc(createdAtUtc);

            Id = id;
            Size = size;
            CreatedAtUtc = createdAtUtc;
            CashReserve = GuardHelper.AgainstNull(
                value: cashReserve,
                propertyName: nameof(CashReserve));
        }

        public HouseholdId Id { get; private set; }
        public HouseholdSize Size { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; }
        public Money CashReserve { get; private set; } = null!;

        public static Household Create(
            HouseholdId id,
            HouseholdSize size,
            DateTimeOffset createdAtUtc,
            Money? cashReserve = null)
        {
            return new Household(
                id: id,
                size: size,
                createdAtUtc: createdAtUtc,
                cashReserve: cashReserve ?? Money.Zero);
        }

        public void Resize(HouseholdSize size)
        {
            Size = size;
        }

        public void ReceiveReserve(Money amount)
        {
            amount = GuardHelper.AgainstNull(
                value: amount,
                propertyName: nameof(amount));

            CashReserve = CashReserve.Add(amount);
        }

        public Money DrainReserve()
        {
            Money drained = CashReserve;
            CashReserve = Money.Zero;
            return drained;
        }

        public Money ReleasePositiveReserveShare(decimal share)
        {
            decimal normalizedShare = Math.Clamp(share, 0m, 1m);
            if (normalizedShare <= 0m || !CashReserve.IsPositive)
                return Money.Zero;

            Money released = CashReserve.Multiply(normalizedShare);
            CashReserve = CashReserve.Subtract(released);
            return released;
        }

        public void ApplyDailyCashflow(
            Money takeHomeIncome,
            Money expenses,
            int daysElapsed)
        {
            takeHomeIncome = GuardHelper.AgainstNull(
                value: takeHomeIncome,
                propertyName: nameof(takeHomeIncome));
            expenses = GuardHelper.AgainstNull(
                value: expenses,
                propertyName: nameof(expenses));

            if (daysElapsed <= 0)
                return;

            CashReserve = CashReserve
               .Add(takeHomeIncome.Multiply(daysElapsed))
               .Subtract(expenses.Multiply(daysElapsed));
        }

        private static void EnsureUtc(DateTimeOffset value)
        {
            GuardHelper.Ensure(
                condition: value.Offset == TimeSpan.Zero,
                value: value,
                errorFactory: DomainErrorsFactory.TimestampMustBeUtc,
                propertyName: nameof(CreatedAtUtc));
        }
    }
}
