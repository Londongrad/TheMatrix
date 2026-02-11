using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Entities
{
    public sealed class ClassicCityHouseholdPlacement
    {
        private ClassicCityHouseholdPlacement() { }

        private ClassicCityHouseholdPlacement(
            HouseholdId householdId,
            CityId cityId,
            DistrictId? districtId,
            ResidentialBuildingId? residentialBuildingId,
            HousingStatus housingStatus)
        {
            HouseholdId = householdId;
            CityId = cityId;
            DistrictId = districtId;
            ResidentialBuildingId = residentialBuildingId;
            HousingStatus = GuardHelper.AgainstInvalidEnum(
                value: housingStatus,
                propertyName: nameof(HousingStatus));

            EnsureHousingConsistency();
        }

        public HouseholdId HouseholdId { get; private set; }
        public CityId CityId { get; private set; }
        public DistrictId? DistrictId { get; private set; }
        public ResidentialBuildingId? ResidentialBuildingId { get; private set; }
        public HousingStatus HousingStatus { get; private set; }

        public static ClassicCityHouseholdPlacement CreateHoused(
            HouseholdId householdId,
            CityId cityId,
            DistrictId districtId,
            ResidentialBuildingId residentialBuildingId)
        {
            return new ClassicCityHouseholdPlacement(
                householdId: householdId,
                cityId: cityId,
                districtId: districtId,
                residentialBuildingId: residentialBuildingId,
                housingStatus: HousingStatus.Housed);
        }

        public static ClassicCityHouseholdPlacement CreateHomeless(
            HouseholdId householdId,
            CityId cityId)
        {
            return new ClassicCityHouseholdPlacement(
                householdId: householdId,
                cityId: cityId,
                districtId: null,
                residentialBuildingId: null,
                housingStatus: HousingStatus.Homeless);
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

        public void BecomeHomeless(CityId cityId)
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
                if (!DistrictId.HasValue || !ResidentialBuildingId.HasValue)
                    throw DomainErrorsFactory.HousedHouseholdRequiresPlacement();

                return;
            }

            if (ResidentialBuildingId.HasValue)
                throw DomainErrorsFactory.HomelessHouseholdCannotHaveResidentialBuilding();
        }
    }
}
