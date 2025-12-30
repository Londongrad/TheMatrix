using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed class MaritalInfo
    {
        private MaritalInfo() { }

        private MaritalInfo(
            MaritalStatus status,
            PersonId? spouseId)
        {
            MaritalRules.ValidateStatusCombination(
                status: status,
                spouseId: spouseId);

            Status = status;
            SpouseId = spouseId;
        }

        public MaritalStatus Status { get; }
        public PersonId? SpouseId { get; }

        public static MaritalInfo FromStatus(
            MaritalStatus status,
            PersonId? spouseId)
        {
            return new MaritalInfo(
                status: status,
                spouseId: spouseId);
        }

        public static MaritalInfo Single()
        {
            return new MaritalInfo(
                status: MaritalStatus.Single,
                spouseId: null);
        }

        public static MaritalInfo Widowed()
        {
            return new MaritalInfo(
                status: MaritalStatus.Widowed,
                spouseId: null);
        }

        public static MaritalInfo MarriedWith(PersonId spouseId)
        {
            return new MaritalInfo(
                status: MaritalStatus.Married,
                spouseId: spouseId);
        }
    }
}
