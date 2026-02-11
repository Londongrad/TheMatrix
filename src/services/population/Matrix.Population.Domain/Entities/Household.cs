using Matrix.BuildingBlocks.Domain;
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
            DateTimeOffset createdAtUtc)
        {
            EnsureUtc(createdAtUtc);

            Id = id;
            Size = size;
            CreatedAtUtc = createdAtUtc;
        }

        public HouseholdId Id { get; private set; }
        public HouseholdSize Size { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; }

        public static Household Create(
            HouseholdId id,
            HouseholdSize size,
            DateTimeOffset createdAtUtc)
        {
            return new Household(
                id: id,
                size: size,
                createdAtUtc: createdAtUtc);
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
