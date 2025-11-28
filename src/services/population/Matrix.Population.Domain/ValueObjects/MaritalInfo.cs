using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed class MaritalInfo
    {
        public MaritalStatus Status { get; }
        public PersonId? SpouseId { get; }

        private MaritalInfo(MaritalStatus status, PersonId? spouseId)
        {
            Status = GuardHelper.AgainstInvalidEnum(status, nameof(status));
            SpouseId = spouseId;

            MaritalRules.ValidateStatusCombination(Status, SpouseId);
        }

        public static MaritalInfo Single() =>
            new(MaritalStatus.Single, null);

        public static MaritalInfo Widowed() =>
            new(MaritalStatus.Widowed, null);

        public static MaritalInfo MarriedWith(PersonId spouseId) =>
            new(MaritalStatus.Married, spouseId);
    }
}
