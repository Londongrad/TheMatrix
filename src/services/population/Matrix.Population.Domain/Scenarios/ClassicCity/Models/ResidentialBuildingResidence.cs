using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Models
{
    public sealed record ResidentialBuildingResidence
    {
        public ResidentialBuildingResidence(
            ResidentialBuildingId residentialBuildingId,
            DistrictId districtId,
            int residentCapacity)
        {
            if (residentCapacity <= 0)
                throw DomainErrorsFactory.ResidentialCapacityOutOfRange();

            ResidentialBuildingId = residentialBuildingId;
            DistrictId = districtId;
            ResidentCapacity = residentCapacity;
        }

        public ResidentialBuildingId ResidentialBuildingId { get; }
        public DistrictId DistrictId { get; }
        public int ResidentCapacity { get; }
    }
}
