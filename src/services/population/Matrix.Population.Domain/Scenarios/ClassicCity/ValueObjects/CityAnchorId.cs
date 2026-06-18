using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.ValueObjects;

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

        public static implicit operator LocationAnchorId(CityAnchorId cityAnchorId)
        {
            return LocationAnchorId.From(cityAnchorId.Value);
        }

        public static implicit operator CityAnchorId(LocationAnchorId locationAnchorId)
        {
            return From(locationAnchorId.Value);
        }
    }
}
