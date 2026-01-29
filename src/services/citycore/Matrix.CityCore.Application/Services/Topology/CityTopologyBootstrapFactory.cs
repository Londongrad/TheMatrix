using Matrix.CityCore.Application.Services.Topology.Abstractions;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Topology;
using Matrix.CityCore.Domain.Topology.Enums;

namespace Matrix.CityCore.Application.Services.Topology
{
    /// <summary>
    ///     Creates a deterministic starter topology for a newly created city.
    /// </summary>
    public sealed class CityTopologyBootstrapFactory : ICityTopologyBootstrapFactory
    {
        public CityTopologySeed CreateInitial(City city)
        {
            ArgumentNullException.ThrowIfNull(city);

            DateTimeOffset createdAtUtc = city.CreatedAtUtc;

            District centralDistrict = District.Create(
                cityId: city.Id,
                name: new DistrictName("Central District"),
                createdAtUtc: createdAtUtc);

            District northDistrict = District.Create(
                cityId: city.Id,
                name: new DistrictName("North District"),
                createdAtUtc: createdAtUtc);

            District southDistrict = District.Create(
                cityId: city.Id,
                name: new DistrictName("South District"),
                createdAtUtc: createdAtUtc);

            var districts = new List<District>
            {
                centralDistrict,
                northDistrict,
                southDistrict
            };

            var buildings = new List<ResidentialBuilding>
            {
                ResidentialBuilding.Create(
                    cityId: city.Id,
                    districtId: centralDistrict.Id,
                    name: new ResidentialBuildingName("Central Tower A"),
                    type: ResidentialBuildingType.Tower,
                    residentCapacity: ResidentCapacity.From(240),
                    createdAtUtc: createdAtUtc),
                ResidentialBuilding.Create(
                    cityId: city.Id,
                    districtId: centralDistrict.Id,
                    name: new ResidentialBuildingName("Central Tower B"),
                    type: ResidentialBuildingType.Tower,
                    residentCapacity: ResidentCapacity.From(240),
                    createdAtUtc: createdAtUtc),
                ResidentialBuilding.Create(
                    cityId: city.Id,
                    districtId: centralDistrict.Id,
                    name: new ResidentialBuildingName("Central Block 1"),
                    type: ResidentialBuildingType.ApartmentBlock,
                    residentCapacity: ResidentCapacity.From(160),
                    createdAtUtc: createdAtUtc),
                ResidentialBuilding.Create(
                    cityId: city.Id,
                    districtId: centralDistrict.Id,
                    name: new ResidentialBuildingName("Central Block 2"),
                    type: ResidentialBuildingType.ApartmentBlock,
                    residentCapacity: ResidentCapacity.From(160),
                    createdAtUtc: createdAtUtc),
                ResidentialBuilding.Create(
                    cityId: city.Id,
                    districtId: northDistrict.Id,
                    name: new ResidentialBuildingName("North Block 1"),
                    type: ResidentialBuildingType.ApartmentBlock,
                    residentCapacity: ResidentCapacity.From(120),
                    createdAtUtc: createdAtUtc),
                ResidentialBuilding.Create(
                    cityId: city.Id,
                    districtId: northDistrict.Id,
                    name: new ResidentialBuildingName("North Block 2"),
                    type: ResidentialBuildingType.ApartmentBlock,
                    residentCapacity: ResidentCapacity.From(120),
                    createdAtUtc: createdAtUtc),
                ResidentialBuilding.Create(
                    cityId: city.Id,
                    districtId: northDistrict.Id,
                    name: new ResidentialBuildingName("North Block 3"),
                    type: ResidentialBuildingType.ApartmentBlock,
                    residentCapacity: ResidentCapacity.From(120),
                    createdAtUtc: createdAtUtc),
                ResidentialBuilding.Create(
                    cityId: city.Id,
                    districtId: southDistrict.Id,
                    name: new ResidentialBuildingName("South Block 1"),
                    type: ResidentialBuildingType.ApartmentBlock,
                    residentCapacity: ResidentCapacity.From(120),
                    createdAtUtc: createdAtUtc),
                ResidentialBuilding.Create(
                    cityId: city.Id,
                    districtId: southDistrict.Id,
                    name: new ResidentialBuildingName("South Block 2"),
                    type: ResidentialBuildingType.ApartmentBlock,
                    residentCapacity: ResidentCapacity.From(120),
                    createdAtUtc: createdAtUtc),
                ResidentialBuilding.Create(
                    cityId: city.Id,
                    districtId: southDistrict.Id,
                    name: new ResidentialBuildingName("South Block 3"),
                    type: ResidentialBuildingType.ApartmentBlock,
                    residentCapacity: ResidentCapacity.From(120),
                    createdAtUtc: createdAtUtc)
            };

            return new CityTopologySeed(
                Districts: districts,
                ResidentialBuildings: buildings);
        }
    }
}