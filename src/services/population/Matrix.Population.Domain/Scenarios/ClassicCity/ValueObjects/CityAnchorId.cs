using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects
{
    public readonly record struct CityAnchorId
    {
        private CityAnchorId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(CityAnchorId));
        }

        public Guid Value { get; }

        public static CityAnchorId From(Guid value)
        {
            return new CityAnchorId(value);
        }
    }
}
