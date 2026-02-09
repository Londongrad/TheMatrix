using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Common;
using Matrix.CityCore.Domain.Errors;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Topology.Enums;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Topology
{
    /// <summary>
    ///     Residential building aggregate root that provides physical housing capacity inside a district.
    /// </summary>
    public sealed class ResidentialBuilding : AggregateRoot<ResidentialBuildingId>
    {
        private ResidentialBuilding(
            ResidentialBuildingId id,
            CityId cityId,
            DistrictId districtId,
            ResidentialBuildingName name,
            ResidentialBuildingType type,
            ResidentCapacity residentCapacity,
            DateTimeOffset createdAtUtc)
            : base(id)
        {
            EnsureUtc(createdAtUtc);

            CityId = cityId;
            DistrictId = districtId;
            Name = name;
            Type = type;
            ResidentCapacity = residentCapacity;
            CreatedAtUtc = createdAtUtc;
        }

        private ResidentialBuilding()
            : base(default(ResidentialBuildingId))
        {
            Name = default(ResidentialBuildingName);
        }

        public CityId CityId { get; private set; }
        public DistrictId DistrictId { get; private set; }
        public ResidentialBuildingName Name { get; private set; }
        public ResidentialBuildingType Type { get; private set; }
        public ResidentCapacity ResidentCapacity { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; }

        public static ResidentialBuilding Create(
            CityId cityId,
            DistrictId districtId,
            ResidentialBuildingName name,
            ResidentialBuildingType type,
            ResidentCapacity residentCapacity,
            DateTimeOffset createdAtUtc)
        {
            GuardHelper.AgainstEmptyGuid(
                id: cityId.Value,
                propertyName: nameof(cityId));
            GuardHelper.AgainstEmptyGuid(
                id: districtId.Value,
                propertyName: nameof(districtId));
            GuardHelper.AgainstInvalidEnum(
                value: type,
                propertyName: nameof(type));

            return new ResidentialBuilding(
                id: ResidentialBuildingId.New(),
                cityId: cityId,
                districtId: districtId,
                name: name,
                type: type,
                residentCapacity: residentCapacity,
                createdAtUtc: createdAtUtc);
        }

        public void Rename(ResidentialBuildingName newName)
        {
            if (newName.Equals(Name))
                return;

            Name = newName;
        }

        public void ChangeResidentCapacity(ResidentCapacity newResidentCapacity)
        {
            if (newResidentCapacity.Equals(ResidentCapacity))
                return;

            ResidentCapacity = newResidentCapacity;
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
