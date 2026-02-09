using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Common;
using Matrix.CityCore.Domain.Errors;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Topology
{
    /// <summary>
    ///     District aggregate root that represents a named area of a city.
    /// </summary>
    public sealed class District : AggregateRoot<DistrictId>
    {
        private District(
            DistrictId id,
            CityId cityId,
            DistrictName name,
            DateTimeOffset createdAtUtc)
            : base(id)
        {
            EnsureUtc(createdAtUtc);

            CityId = cityId;
            Name = name;
            CreatedAtUtc = createdAtUtc;
        }

        private District()
            : base(default(DistrictId))
        {
            Name = default(DistrictName);
        }

        public CityId CityId { get; private set; }
        public DistrictName Name { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; }

        public static District Create(
            CityId cityId,
            DistrictName name,
            DateTimeOffset createdAtUtc)
        {
            GuardHelper.AgainstEmptyGuid(
                id: cityId.Value,
                propertyName: nameof(cityId));

            return new District(
                id: DistrictId.New(),
                cityId: cityId,
                name: name,
                createdAtUtc: createdAtUtc);
        }

        public void Rename(DistrictName newName)
        {
            if (newName.Equals(Name))
                return;

            Name = newName;
        }

        private static void EnsureUtc(DateTimeOffset value)
        {
            GuardHelper.Ensure(
                condition: value.Offset == TimeSpan.Zero,
                value: value,
                errorFactory: DomainErrorsFactory.TopologyTimestampMustBeUtc);
        }
    }
}
