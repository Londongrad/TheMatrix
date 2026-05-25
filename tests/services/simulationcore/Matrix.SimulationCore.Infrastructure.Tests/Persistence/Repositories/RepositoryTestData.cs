using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Profiles;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World.Enums;
using Matrix.SimulationCore.Domain.Simulation;
using Matrix.SimulationCore.Infrastructure.Tests.Services.Simulation;

namespace Matrix.SimulationCore.Infrastructure.Tests.Persistence.Repositories
{
    internal static class RepositoryTestData
    {
        internal static readonly DateTimeOffset BaseUtc = new(
            year: 2048,
            month: 2,
            day: 3,
            hour: 4,
            minute: 5,
            second: 6,
            offset: TimeSpan.Zero);

        internal static District CreateDistrict(
            CityId cityId,
            string name = "Downtown",
            decimal anchorX = 12.3456m,
            decimal anchorY = 45.6784m,
            DateTimeOffset? createdAtUtc = null)
        {
            return District.Create(
                cityId: cityId,
                name: new DistrictName(name),
                anchorX: anchorX,
                anchorY: anchorY,
                createdAtUtc: createdAtUtc ?? BaseUtc);
        }

        internal static RoadNode CreateRoadNode(
            CityId cityId,
            DistrictId districtId,
            string name = "North Junction",
            RoadNodeType type = RoadNodeType.Junction,
            decimal positionX = 18.7654m,
            decimal positionY = 72.1116m,
            DateTimeOffset? createdAtUtc = null)
        {
            return RoadNode.Create(
                cityId: cityId,
                districtId: districtId,
                name: name,
                type: type,
                positionX: positionX,
                positionY: positionY,
                createdAtUtc: createdAtUtc ?? BaseUtc);
        }

        internal static RoadSegment CreateRoadSegment(
            CityId cityId,
            DistrictId districtId,
            RoadNodeId fromRoadNodeId,
            RoadNodeId toRoadNodeId,
            string name = "Main Artery",
            RoadSegmentType type = RoadSegmentType.Arterial,
            decimal lengthMeters = 154.555m,
            DateTimeOffset? createdAtUtc = null)
        {
            return RoadSegment.Create(
                cityId: cityId,
                districtId: districtId,
                fromRoadNodeId: fromRoadNodeId,
                toRoadNodeId: toRoadNodeId,
                name: name,
                type: type,
                lengthMeters: lengthMeters,
                createdAtUtc: createdAtUtc ?? BaseUtc);
        }

        internal static CityAnchor CreateCityAnchor(
            CityId cityId,
            DistrictId districtId,
            RoadNodeId accessRoadNodeId,
            string name = "Central Hospital",
            CityAnchorType type = CityAnchorType.Hospital,
            int capacity = 1200,
            decimal positionX = 22.3456m,
            decimal positionY = 55.4321m,
            DateTimeOffset? createdAtUtc = null)
        {
            return CityAnchor.Create(
                cityId: cityId,
                districtId: districtId,
                accessRoadNodeId: accessRoadNodeId,
                name: new CityAnchorName(name),
                type: type,
                capacity: capacity,
                positionX: positionX,
                positionY: positionY,
                createdAtUtc: createdAtUtc ?? BaseUtc);
        }

        internal static ResidentialBuilding CreateResidentialBuilding(
            CityId cityId,
            DistrictId districtId,
            RoadNodeId accessRoadNodeId,
            string name = "Tower A",
            ResidentialBuildingType type = ResidentialBuildingType.Tower,
            int residentCapacity = 380,
            decimal positionX = 40.1256m,
            decimal positionY = 60.4444m,
            DateTimeOffset? createdAtUtc = null)
        {
            return ResidentialBuilding.Create(
                cityId: cityId,
                districtId: districtId,
                accessRoadNodeId: accessRoadNodeId,
                name: new ResidentialBuildingName(name),
                type: type,
                residentCapacity: ResidentCapacity.From(residentCapacity),
                positionX: positionX,
                positionY: positionY,
                createdAtUtc: createdAtUtc ?? BaseUtc);
        }

        internal static WeatherState CreateWeatherState(
            SimTime startedAt,
            SimTime expectedUntil,
            WeatherType type = WeatherType.Clear,
            PrecipitationKind precipitationKind = PrecipitationKind.None,
            WeatherSeverity severity = WeatherSeverity.Calm)
        {
            return WeatherState.Create(
                type: type,
                severity: severity,
                precipitationKind: precipitationKind,
                temperature: TemperatureC.From(18m),
                humidity: HumidityPercent.From(45m),
                windSpeed: WindSpeedKph.From(12m),
                cloudCoverage: CloudCoveragePercent.From(10m),
                pressure: PressureHpa.From(1013m),
                startedAt: startedAt,
                expectedUntil: expectedUntil);
        }

        internal static WeatherClimateProfile CreateClimateProfile()
        {
            return WeatherClimateProfile.Create(
                climateZone: ClimateZone.Temperate,
                temperatureProfile: SeasonalTemperatureProfile.Create(
                    springAverage: TemperatureC.From(12m),
                    summerAverage: TemperatureC.From(24m),
                    autumnAverage: TemperatureC.From(10m),
                    winterAverage: TemperatureC.From(-6m),
                    dailySwing: TemperatureC.From(7m)),
                precipitationProfile: SeasonalPrecipitationProfile.Create(
                    springHumidity: HumidityPercent.From(58m),
                    summerHumidity: HumidityPercent.From(62m),
                    autumnHumidity: HumidityPercent.From(70m),
                    winterHumidity: HumidityPercent.From(77m),
                    springDominantKind: PrecipitationKind.Rain,
                    summerDominantKind: PrecipitationKind.Rain,
                    autumnDominantKind: PrecipitationKind.Drizzle,
                    winterDominantKind: PrecipitationKind.Snow),
                windProfile: SeasonalWindProfile.Create(
                    springAverage: WindSpeedKph.From(16m),
                    summerAverage: WindSpeedKph.From(12m),
                    autumnAverage: WindSpeedKph.From(19m),
                    winterAverage: WindSpeedKph.From(23m),
                    gustHeadroom: WindSpeedKph.From(31m)),
                volatility: WeatherVolatility.From(0.25m),
                extremeWeatherProfile: ExtremeWeatherProfile.Create(
                    maxOverallSeverity: WeatherSeverity.Extreme,
                    supportsThunderstorms: true,
                    supportsSnowstorms: true,
                    supportsFog: true,
                    supportsHeatwaves: true));
        }

        internal static CityWeather CreateCityWeather(
            CityId cityId,
            SimTime? createdAt = null,
            WeatherState? currentState = null)
        {
            SimTime effectiveCreatedAt = createdAt ?? SimTime.FromUtc(BaseUtc.AddHours(2));
            var startedAt = SimTime.FromUtc(BaseUtc.AddHours(1));
            SimTime expectedUntil = startedAt.Add(TimeSpan.FromHours(3));

            return CityWeather.Create(
                cityId: cityId,
                climateProfile: CreateClimateProfile(),
                currentState: currentState ??
                CreateWeatherState(
                    startedAt: startedAt,
                    expectedUntil: expectedUntil),
                createdAt: effectiveCreatedAt);
        }

        internal static CityActiveTripSegment CreateTripSegment(
            int sequence,
            RoadSegmentId roadSegmentId,
            DistrictId districtId,
            RoadNodeId fromRoadNodeId,
            RoadNodeId toRoadNodeId,
            string name,
            string type,
            decimal lengthMeters,
            decimal estimatedTraversalMinutes,
            decimal fromPositionX,
            decimal fromPositionY,
            decimal toPositionX,
            decimal toPositionY)
        {
            return CityActiveTripSegment.Create(
                sequence: sequence,
                roadSegmentId: roadSegmentId,
                districtId: districtId,
                fromRoadNodeId: fromRoadNodeId,
                toRoadNodeId: toRoadNodeId,
                name: name,
                type: type,
                lengthMeters: lengthMeters,
                estimatedTraversalMinutes: estimatedTraversalMinutes,
                fromPositionX: fromPositionX,
                fromPositionY: fromPositionY,
                toPositionX: toPositionX,
                toPositionY: toPositionY);
        }

        internal static CityActiveTrip CreateTrip(
            CityId cityId,
            DistrictId fromDistrictId,
            DistrictId toDistrictId,
            RoadNodeId fromRoadNodeId,
            RoadNodeId midRoadNodeId,
            RoadNodeId toRoadNodeId,
            RoadSegmentId firstRoadSegmentId,
            RoadSegmentId secondRoadSegmentId,
            DateTimeOffset? startedAtSimTimeUtc = null,
            string subject = "Resident commute",
            Guid? travellerEntityId = null)
        {
            IReadOnlyCollection<CityActiveTripSegment> segments =
            [
                CreateTripSegment(
                    sequence: 0,
                    roadSegmentId: firstRoadSegmentId,
                    districtId: fromDistrictId,
                    fromRoadNodeId: fromRoadNodeId,
                    toRoadNodeId: midRoadNodeId,
                    name: "Segment A",
                    type: "arterial",
                    lengthMeters: 120m,
                    estimatedTraversalMinutes: 6m,
                    fromPositionX: 10.1111m,
                    fromPositionY: 20.2222m,
                    toPositionX: 30.3333m,
                    toPositionY: 40.4444m)
            ];

            return CityActiveTrip.Create(
                cityId: cityId,
                travellerEntityId: travellerEntityId ?? Guid.NewGuid(),
                subject: subject,
                purpose: CityTripPurpose.WorkCommute,
                profile: "pedestrian",
                movementCapabilityIndex: 1m,
                usedDynamicRoadConditions: true,
                plannedAtTickId: 42,
                conditionsEffectiveTickId: 40,
                startedAtSimTimeUtc: startedAtSimTimeUtc ?? BaseUtc.AddHours(3),
                fromKind: "district",
                fromEntityId: Guid.NewGuid(),
                fromDistrictId: fromDistrictId,
                fromRoadNodeId: fromRoadNodeId,
                fromName: "Downtown",
                fromPositionX: 10.1111m,
                fromPositionY: 20.2222m,
                toKind: "anchor",
                toEntityId: Guid.NewGuid(),
                toDistrictId: toDistrictId,
                toRoadNodeId: toRoadNodeId,
                toName: "Office Campus",
                toPositionX: 70.7777m,
                toPositionY: 80.8888m,
                totalDistanceMeters: 200m,
                plannedTravelTimeMinutes: 12m,
                segments: segments);
        }

        internal static City CreateCity(
            DateTimeOffset? createdAtUtc = null,
            string name = "Clock City")
        {
            return SimulationInfrastructureTestSupport.CreateCity(
                createdAtUtc: createdAtUtc ?? BaseUtc,
                name: name);
        }
    }
}
