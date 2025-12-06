using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed class MaritalInfo
    {
        private MaritalInfo()
        {
        }

        private MaritalInfo(MaritalStatus status, PersonId? spouseId)
        {
            MaritalRules.ValidateStatusCombination(status: Status, spouseId: SpouseId);

            Status = status;
            SpouseId = spouseId;
        }

        public MaritalStatus Status { get; }
        public PersonId? SpouseId { get; }

        public static MaritalInfo FromStatus(MaritalStatus status, PersonId? spouseId) =>
            new(status: status, spouseId: spouseId);

        public static MaritalInfo Single() =>
            new(status: MaritalStatus.Single, spouseId: null);

        public static MaritalInfo Widowed() =>
            new(status: MaritalStatus.Widowed, spouseId: null);

        public static MaritalInfo MarriedWith(PersonId spouseId) =>
            new(status: MaritalStatus.Married, spouseId: spouseId);
    }
}
