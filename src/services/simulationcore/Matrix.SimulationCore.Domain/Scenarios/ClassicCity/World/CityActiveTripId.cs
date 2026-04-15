using Matrix.BuildingBlocks.Domain;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World
{
    public readonly record struct CityActiveTripId
    {
        public CityActiveTripId(Guid value)
        {
            Value = GuardHelper.AgainstEmptyGuid(
                id: value,
                propertyName: nameof(Value));
        }

        public Guid Value { get; }

        public static CityActiveTripId New()
        {
            return new CityActiveTripId(Guid.NewGuid());
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
