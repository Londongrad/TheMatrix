using System.Net;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity.Integrations.SimulationSystems;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity;
using Xunit;
using static Matrix.Population.Infrastructure.Tests.Http.HttpClientTestSupport;

namespace Matrix.Population.Infrastructure.Tests.Scenarios.ClassicCity.Integrations.SimulationSystems
{
    public sealed class CityDistrictUtilityConditionsClientTests
    {
        [Fact]
        public async Task GetByCityAsync_WhenAllPayloadsExist_MergesDistrictSnapshots()
        {
            var cityId = Guid.Parse("98dbf5c8-68cb-40b1-b0d5-96e093b18503");
            var districtId = Guid.Parse("0f0c9a90-a79d-40d2-9faf-122a70ebf53f");
            List<string> requestedPaths = [];
            HttpClient client = CreateClient((
                request,
                _) =>
            {
                requestedPaths.Add(request.RequestUri!.PathAndQuery);
                string json = request.RequestUri!.PathAndQuery switch
                {
                    var path when path.EndsWith(
                            value: "/heating/districts",
                            comparisonType: StringComparison.Ordinal) =>
                        $$"""
                          {"cityId":"{{cityId}}","effectiveTickId":1,"lastEvaluatedAtUtc":"2048-05-06T08:00:00+00:00","heatingSupportIndex":0.8,"districts":[{"districtId":"{{districtId}}","heatingCoverageIndex":0.7,"heatingSupportIndex":0.8,"outageRiskIndex":0.2,"comfortStressIndex":0.3,"maintenancePriorityIndex":0.4}]}
                          """,
                    var path when path.EndsWith(
                            value: "/water-distribution/districts",
                            comparisonType: StringComparison.Ordinal) =>
                        $$"""
                          {"cityId":"{{cityId}}","effectiveTickId":1,"lastEvaluatedAtUtc":"2048-05-06T08:00:00+00:00","waterSupportIndex":0.9,"districts":[{"districtId":"{{districtId}}","waterCoverageIndex":0.6,"waterSupportIndex":0.9,"disruptionRiskIndex":0.1,"qualityRiskIndex":0.2,"maintenancePriorityIndex":0.3}]}
                          """,
                    var path when path.EndsWith(
                            value: "/power-distribution/districts",
                            comparisonType: StringComparison.Ordinal) =>
                        $$"""
                          {"cityId":"{{cityId}}","effectiveTickId":1,"lastEvaluatedAtUtc":"2048-05-06T08:00:00+00:00","powerSupportIndex":0.95,"districts":[{"districtId":"{{districtId}}","powerCoverageIndex":0.5,"powerSupportIndex":0.95,"outageRiskIndex":0.15,"restorationStrainIndex":0.25,"maintenancePriorityIndex":0.35}]}
                          """,
                    var path when path.EndsWith(
                            value: "/sanitation/districts",
                            comparisonType: StringComparison.Ordinal) =>
                        $$"""
                          {"cityId":"{{cityId}}","effectiveTickId":1,"lastEvaluatedAtUtc":"2048-05-06T08:00:00+00:00","sanitationSupportIndex":0.85,"districts":[{"districtId":"{{districtId}}","sanitationCoverageIndex":0.4,"sanitationSupportIndex":0.85,"overflowRiskIndex":0.3,"contaminationRiskIndex":0.45,"maintenancePriorityIndex":0.55}]}
                          """,
                    var path when path.EndsWith(
                            value: "/utility-incidents/districts",
                            comparisonType: StringComparison.Ordinal) =>
                        $$"""
                          {"cityId":"{{cityId}}","effectiveTickId":1,"lastEvaluatedAtUtc":"2048-05-06T08:00:00+00:00","utilityIncidentSupportIndex":0.88,"districts":[{"districtId":"{{districtId}}","utilityContinuityIndex":0.75,"dispatchReadinessIndex":0.65,"incidentPressureIndex":0.22,"coordinationDifficultyIndex":0.18,"restorationPriorityIndex":0.52}]}
                          """,
                    _ => throw new InvalidOperationException("Unexpected path.")
                };

                return Task.FromResult(JsonResponse(json));
            });
            var utilityClient = new CityDistrictUtilityConditionsClient(client);

            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot> result =
                await utilityClient.GetByCityAsync(
                    cityId: cityId,
                    cancellationToken: CancellationToken.None);

            CityDistrictUtilityConditionsSnapshot snapshot = Assert.Single(result)
               .Value;
            Assert.Equal(
                expected: DistrictId.From(districtId),
                actual: snapshot.DistrictId);
            Assert.Equal(
                expected: 0.7m,
                actual: snapshot.HeatingCoverageIndex);
            Assert.Equal(
                expected: 0.3m,
                actual: snapshot.HeatingComfortStressIndex);
            Assert.Equal(
                expected: 0.6m,
                actual: snapshot.WaterCoverageIndex);
            Assert.Equal(
                expected: 0.1m,
                actual: snapshot.WaterDisruptionRiskIndex);
            Assert.Equal(
                expected: 0.5m,
                actual: snapshot.PowerCoverageIndex);
            Assert.Equal(
                expected: 0.15m,
                actual: snapshot.PowerOutageRiskIndex);
            Assert.Equal(
                expected: 0.4m,
                actual: snapshot.SanitationCoverageIndex);
            Assert.Equal(
                expected: 0.45m,
                actual: snapshot.SanitationContaminationRiskIndex);
            Assert.Equal(
                expected: 0.65m,
                actual: snapshot.UtilityIncidentDispatchReadinessIndex);
            Assert.Equal(
                expected: 0.22m,
                actual: snapshot.UtilityIncidentPressureIndex);
            Assert.Equal(
                expected: 0.18m,
                actual: snapshot.UtilityIncidentCoordinationDifficultyIndex);
            Assert.Equal(
                expected: 0.52m,
                actual: snapshot.UtilityIncidentRestorationPriorityIndex);
            Assert.Equal(
                expected: 5,
                actual: requestedPaths.Count);
            Assert.All(
                collection: requestedPaths,
                action: path => Assert.StartsWith(
                    expectedStartString: $"{ClassicCitySimulationSystemsApiRoutes.CitiesPath}/{cityId}/",
                    actualString: path,
                    comparisonType: StringComparison.Ordinal));
        }

        [Fact]
        public async Task GetByCityAsync_WhenOnePayloadIsNotFound_ReturnsEmptyDictionary()
        {
            HttpClient client = CreateClient((
                request,
                _) =>
            {
                if (request.RequestUri!.PathAndQuery.EndsWith(
                        value: "/water-distribution/districts",
                        comparisonType: StringComparison.Ordinal))
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

                return Task.FromResult(
                    JsonResponse(
                        """{"cityId":"98dbf5c8-68cb-40b1-b0d5-96e093b18503","effectiveTickId":1,"lastEvaluatedAtUtc":"2048-05-06T08:00:00+00:00","districts":[]}"""));
            });
            var utilityClient = new CityDistrictUtilityConditionsClient(client);

            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot> result =
                await utilityClient.GetByCityAsync(
                    cityId: Guid.Parse("98dbf5c8-68cb-40b1-b0d5-96e093b18503"),
                    cancellationToken: CancellationToken.None);

            Assert.Empty(result);
        }
    }
}
