using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Xunit;

namespace Matrix.Population.Domain.Tests.Scenarios.ClassicCity.Services
{
    public sealed class CityPopulationAnchorSelectionPolicyTests
    {
        [Fact]
        public void SelectWorkplaceAnchor_WhenNoTypedAnchorsExist_ReturnsNull()
        {
            var policy = new CityPopulationAnchorSelectionPolicy();

            CityPopulationAnchorCatalogItem? selected = policy.SelectWorkplaceAnchor(
                anchors:
                [
                    CreateAnchor(
                        name: "School A",
                        type: CityAnchorType.School,
                        cityAnchorId: "11111111-0000-0000-0000-000000000001")
                ],
                preferredDistrictId: null,
                stableKey: Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"));

            Assert.Null(selected);
        }

        [Fact]
        public void SelectSchoolAnchor_WhenPreferredAnchorsAreProvided_PrefersThemOverDistrict()
        {
            var policy = new CityPopulationAnchorSelectionPolicy();
            CityPopulationAnchorCatalogItem preferred = CreateAnchor(
                name: "Preferred School",
                type: CityAnchorType.School,
                cityAnchorId: "11111111-0000-0000-0000-000000000010",
                districtId: "aaaaaaaa-0000-0000-0000-000000000001");
            CityPopulationAnchorCatalogItem districtMatch = CreateAnchor(
                name: "District School",
                type: CityAnchorType.School,
                cityAnchorId: "11111111-0000-0000-0000-000000000011",
                districtId: "bbbbbbbb-0000-0000-0000-000000000002");

            CityPopulationAnchorCatalogItem? selected = policy.SelectSchoolAnchor(
                anchors:
                [
                    districtMatch,
                    preferred
                ],
                preferredDistrictId: districtMatch.DistrictId,
                stableKey: Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002"),
                preferredAnchorIds: [preferred.CityAnchorId]);

            Assert.NotNull(selected);
            Assert.Equal(
                expected: preferred.CityAnchorId,
                actual: selected!.CityAnchorId);
        }

        [Fact]
        public void SelectHospitalAnchor_WhenPreferredDistrictExists_SelectsDeterministicallyFromDistrictSubset()
        {
            var policy = new CityPopulationAnchorSelectionPolicy();
            var preferredDistrictId = DistrictId.From(Guid.Parse("bbbbbbbb-0000-0000-0000-000000000003"));
            var stableKey = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");

            CityPopulationAnchorCatalogItem first = CreateAnchor(
                name: "Alpha Hospital",
                type: CityAnchorType.Hospital,
                cityAnchorId: "11111111-0000-0000-0000-000000000020",
                districtId: preferredDistrictId.Value.ToString());
            CityPopulationAnchorCatalogItem second = CreateAnchor(
                name: "Beta Hospital",
                type: CityAnchorType.Hospital,
                cityAnchorId: "11111111-0000-0000-0000-000000000021",
                districtId: preferredDistrictId.Value.ToString());
            CityPopulationAnchorCatalogItem third = CreateAnchor(
                name: "Gamma Hospital",
                type: CityAnchorType.Hospital,
                cityAnchorId: "11111111-0000-0000-0000-000000000022",
                districtId: preferredDistrictId.Value.ToString());
            CityPopulationAnchorCatalogItem fourth = CreateAnchor(
                name: "Omega Hospital",
                type: CityAnchorType.Hospital,
                cityAnchorId: "11111111-0000-0000-0000-000000000023",
                districtId: preferredDistrictId.Value.ToString());
            CityPopulationAnchorCatalogItem otherDistrict = CreateAnchor(
                name: "Far Hospital",
                type: CityAnchorType.Hospital,
                cityAnchorId: "11111111-0000-0000-0000-000000000024",
                districtId: "cccccccc-0000-0000-0000-000000000004");

            CityPopulationAnchorCatalogItem? selected = policy.SelectHospitalAnchor(
                anchors:
                [
                    fourth,
                    second,
                    otherDistrict,
                    first,
                    third
                ],
                preferredDistrictId: preferredDistrictId,
                stableKey: stableKey);
            CityPopulationAnchorCatalogItem? selectedAgain = policy.SelectHospitalAnchor(
                anchors:
                [
                    fourth,
                    second,
                    otherDistrict,
                    first,
                    third
                ],
                preferredDistrictId: preferredDistrictId,
                stableKey: stableKey);

            Assert.NotNull(selected);
            Assert.Equal(
                expected: selected,
                actual: selectedAgain);
            Assert.True(selected != otherDistrict);
            Assert.True(selected == first || selected == second || selected == third || selected == fourth);
        }

        [Fact]
        public void SelectWorkplaceAnchor_WhenNoPreferredDistrictFallsBackToTypedAnchors()
        {
            var policy = new CityPopulationAnchorSelectionPolicy();
            var stableKey = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000004");
            CityPopulationAnchorCatalogItem alpha = CreateAnchor(
                name: "Alpha Workplace",
                type: CityAnchorType.Workplace,
                cityAnchorId: "11111111-0000-0000-0000-000000000030",
                districtId: "dddddddd-0000-0000-0000-000000000005");
            CityPopulationAnchorCatalogItem beta = CreateAnchor(
                name: "Beta Workplace",
                type: CityAnchorType.Workplace,
                cityAnchorId: "11111111-0000-0000-0000-000000000031",
                districtId: "eeeeeeee-0000-0000-0000-000000000006");

            CityPopulationAnchorCatalogItem? selected = policy.SelectWorkplaceAnchor(
                anchors:
                [
                    beta,
                    alpha
                ],
                preferredDistrictId: DistrictId.From(Guid.Parse("ffffffff-0000-0000-0000-000000000007")),
                stableKey: stableKey);

            Assert.NotNull(selected);
            Assert.True(selected == alpha || selected == beta);
        }

        private static CityPopulationAnchorCatalogItem CreateAnchor(
            string name,
            CityAnchorType type,
            string cityAnchorId,
            string? districtId = null)
        {
            return CityPopulationAnchorCatalogItem.Create(
                cityId: CityId.From(Guid.Parse("67676767-6767-6767-6767-676767676767")),
                cityAnchorId: CityAnchorId.From(Guid.Parse(cityAnchorId)),
                districtId: DistrictId.From(Guid.Parse(districtId ?? "abababab-0000-0000-0000-000000000001")),
                accessRoadNodeId: RoadNodeId.From(Guid.Parse("cdcdcdcd-0000-0000-0000-000000000001")),
                name: name,
                type: type,
                capacity: 10,
                positionX: 1m,
                positionY: 2m,
                createdAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 2,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }
    }
}
