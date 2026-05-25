using Matrix.BuildingBlocks.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Authorization.Permissions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.GetCityDistrictHeatingConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.
    GetCityDistrictPowerDistributionConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using
    Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.GetCityDistrictSanitationConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.
    GetCityDistrictUtilityIncidentConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.
    GetCityDistrictWaterDistributionConditions;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases
{
    public sealed class ClassicCityConditionQueryPermissionTests
    {
        public static TheoryData<IRequirePermission> Queries => new()
        {
            new GetCityDistrictHeatingConditionsQuery(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            new GetCityDistrictPowerDistributionConditionsQuery(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            new GetCityRoadSegmentConditionsQuery(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            new GetCityDistrictSanitationConditionsQuery(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            new GetCityDistrictUtilityIncidentConditionsQuery(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
            new GetCityDistrictWaterDistributionConditionsQuery(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"))
        };

        [Theory]
        [MemberData(nameof(Queries))]
        public void Query_RequiresSimulationSystemsClassicCityReadPermission(IRequirePermission query)
        {
            Assert.Equal(
                expected: PermissionKeys.SimulationSystemsClassicCityRead,
                actual: query.PermissionKey);
        }
    }
}
