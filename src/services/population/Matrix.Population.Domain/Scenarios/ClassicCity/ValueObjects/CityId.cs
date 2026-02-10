using Matrix.BuildingBlocks.Domain;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects
{
    public readonly record struct CityId
    {
        private CityId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(CityId));
        }

        public Guid Value { get; }

        public static CityId From(Guid value)
        {
            return new CityId(value);
        }
    }
}
