using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Rules
{
    public static class AgeGroupRules
    {
        public static AgeGroup GetAgeGroup(Age age)
        {
            if (age.Years < 7)
                return AgeGroup.Child;
            if (age.Years < 17)
                return AgeGroup.Youth;
            if (age.Years < 66)
                return AgeGroup.Adult;
            return AgeGroup.Senior;
        }
    }
}
