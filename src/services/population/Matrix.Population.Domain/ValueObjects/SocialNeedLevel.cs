using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.ValueObjects
{
    public readonly record struct SocialNeedLevel
    {
        public const int MinSocialNeed = 0;
        public const int MaxSocialNeed = 100;

        public SocialNeedLevel(int value)
        {
            Value = GuardHelper.AgainstOutOfRange(
                value: value,
                min: MinSocialNeed,
                max: MaxSocialNeed,
                propertyName: nameof(SocialNeedLevel));
        }

        public int Value { get; }

        public static SocialNeedLevel From(int value)
        {
            return new SocialNeedLevel(value);
        }

        public static SocialNeedLevel Default()
        {
            return From(35);
        }

        public SocialNeedLevel WithDelta(int delta)
        {
            return From(
                Math.Clamp(
                    value: Value + delta,
                    min: MinSocialNeed,
                    max: MaxSocialNeed));
        }
    }
}
