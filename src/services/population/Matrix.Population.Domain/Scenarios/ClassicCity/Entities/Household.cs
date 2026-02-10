using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Entities
{
    public sealed class Household
    {
        private Household() { }

        private Household(
            HouseholdId id,
            CityId? cityId,
            DistrictId? districtId,
            ResidentialBuildingId? residentialBuildingId,
            HousingStatus housingStatus,
            HouseholdSize size,
            DateTimeOffset createdAtUtc)
        {
            EnsureUtc(createdAtUtc);

            Id = id;
            CityId = cityId;
            DistrictId = districtId;
            ResidentialBuildingId = residentialBuildingId;
            HousingStatus = GuardHelper.AgainstInvalidEnum(
                value: housingStatus,
                propertyName: nameof(HousingStatus));
            Size = size;
            CreatedAtUtc = createdAtUtc;

            EnsureHousingConsistency();
        }

        public HouseholdId Id { get; private set; }
        public CityId? CityId { get; private set; }
        public DistrictId? DistrictId { get; private set; }
        public ResidentialBuildingId? ResidentialBuildingId { get; private set; }
        public HousingStatus HousingStatus { get; private set; }
        public HouseholdSize Size { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; }

        public static Household CreateHoused(
            HouseholdId id,
            CityId cityId,
            DistrictId districtId,
            ResidentialBuildingId residentialBuildingId,
            HouseholdSize size,
            DateTimeOffset createdAtUtc)
        {
            return new Household(
                id: id,
                cityId: cityId,
                districtId: districtId,
                residentialBuildingId: residentialBuildingId,
                housingStatus: HousingStatus.Housed,
                size: size,
                createdAtUtc: createdAtUtc);
        }

        public static Household CreateHomeless(
            HouseholdId id,
            HouseholdSize size,
            DateTimeOffset createdAtUtc,
            CityId? cityId = null)
        {
            return new Household(
                id: id,
                cityId: cityId,
                districtId: null,
                residentialBuildingId: null,
                housingStatus: HousingStatus.Homeless,
                size: size,
                createdAtUtc: createdAtUtc);
        }

        public void Relocate(
            CityId cityId,
            DistrictId districtId,
            ResidentialBuildingId residentialBuildingId)
        {
            CityId = cityId;
            DistrictId = districtId;
            ResidentialBuildingId = residentialBuildingId;
            HousingStatus = HousingStatus.Housed;

            EnsureHousingConsistency();
        }

        public void BecomeHomeless(CityId? cityId = null)
        {
            CityId = cityId;
            DistrictId = null;
            ResidentialBuildingId = null;
            HousingStatus = HousingStatus.Homeless;

            EnsureHousingConsistency();
        }

        private void EnsureHousingConsistency()
        {
            if (HousingStatus == HousingStatus.Housed)
            {
                if (!CityId.HasValue || !DistrictId.HasValue || !ResidentialBuildingId.HasValue)
                    throw DomainErrorsFactory.HousedHouseholdRequiresPlacement();

                return;
            }

            if (ResidentialBuildingId.HasValue)
                throw DomainErrorsFactory.HomelessHouseholdCannotHaveResidentialBuilding();
        }

        private static void EnsureUtc(DateTimeOffset value)
        {
            GuardHelper.Ensure(
                condition: value.Offset == TimeSpan.Zero,
                value: value,
                errorFactory: DomainErrorsFactory.TimestampMustBeUtc,
                propertyName: nameof(CreatedAtUtc));
        }
    }
}
