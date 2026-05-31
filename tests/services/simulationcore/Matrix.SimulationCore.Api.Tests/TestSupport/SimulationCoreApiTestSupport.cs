using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Models.Provisioning;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.Common;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetGenerationCatalog;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetSuggestedCityNames;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityAnchors;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityDistricts;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityMapTopology;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityResidentialBuildings;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityRoadGraph;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Weather.GetWeather;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.Common;
using Matrix.SimulationCore.Application.UseCases.Simulation.GetClock;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Requests;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Routing.Requests;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Trips.Requests;
using Matrix.SimulationCore.Domain.Simulation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Matrix.SimulationCore.Api.Tests.TestSupport
{
    public static class SimulationCoreApiTestSupport
    {
        public static ClockDto CreateClockDto(
            Guid? simulationId = null,
            Guid? hostId = null)
        {
            return new ClockDto(
                SimulationId: simulationId ?? Guid.Parse("7a0962bb-d842-4e0a-9f4b-4f12c08c7efd"),
                HostId: hostId ?? Guid.Parse("fd0f9006-a204-4ba1-a0de-42ff5dfb90bb"),
                ScenarioKey: "classic-city",
                HostTypeKey: "city",
                SimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 9,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero),
                TickId: 42,
                Speed: 2.0m,
                State: ClockState.Running);
        }

        public static CreateCityRequest CreateCreateCityRequest()
        {
            return new CreateCityRequest(
                Name: "Mega City",
                ClimateZone: "Continental",
                Hemisphere: "Northern",
                UtcOffsetMinutes: 180,
                GenerationSeed: "seed-42",
                SizeTier: "Large",
                UrbanDensity: "Dense",
                DevelopmentLevel: "Developed",
                EconomyProfile: "Balanced",
                PopulationOccupancyProfile: "Stable",
                InitialWeatherMode: "Seasonal",
                InitialWeatherType: "Clear",
                InitialWeatherSeverity: "None",
                InitialWeatherTemperatureC: 18.5m,
                StartSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                SpeedMultiplier: 1.5m,
                PlannedPeopleCount: 120,
                ProvisioningCorrelationId: Guid.Parse("927527e6-c584-4ecf-bb1c-a1b9a01cfa47"),
                ScenarioModelSetVersion: "classic-city-v2");
        }

        public static CityCreatedDto CreateCityCreatedDto(Guid? cityId = null)
        {
            return new CityCreatedDto(
                CityId: cityId ?? Guid.Parse("71b8b30b-d43c-4111-b1da-02c4d258cb18"),
                PopulationBootstrapOperationId: Guid.Parse("f91e16fd-ee76-4330-8dda-3fb5b8749d52"),
                EconomyBootstrapOperationId: Guid.Parse("2c03f709-08b6-4d6c-8789-f45f5ecdd3a3"));
        }

        public static CityDto CreateCityDto(
            Guid? cityId = null,
            string name = "Mega City",
            string status = "Provisioning")
        {
            Guid effectiveCityId = cityId ?? Guid.Parse("71b8b30b-d43c-4111-b1da-02c4d258cb18");
            DateTimeOffset now = new(
                year: 2048,
                month: 6,
                day: 1,
                hour: 10,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);

            return new CityDto(
                CityId: effectiveCityId,
                SimulationId: effectiveCityId,
                Name: name,
                SimulationKind: "ClassicCity",
                Status: status,
                ClimateZone: "Continental",
                Hemisphere: "Northern",
                UtcOffsetMinutes: 180,
                GenerationSeed: "seed-42",
                RunId: Guid.Parse("c71f5ee2-dc03-4db6-b04a-d8db3c621f8b"),
                ScenarioModelSetVersion: "classic-city-v2",
                SizeTier: "Large",
                UrbanDensity: "Dense",
                DevelopmentLevel: "Developed",
                EconomyProfile: "Balanced",
                PopulationOccupancyProfile: "Stable",
                PlannedPeopleCount: 120,
                PopulationBootstrapOperationId: Guid.Parse("f91e16fd-ee76-4330-8dda-3fb5b8749d52"),
                EconomyBootstrapOperationId: Guid.Parse("2c03f709-08b6-4d6c-8789-f45f5ecdd3a3"),
                CreatedAtUtc: now.AddHours(-1),
                PopulationBootstrapCompletedAtUtc: now,
                EconomyBootstrapCompletedAtUtc: now,
                PopulationBootstrapFailedAtUtc: null,
                EconomyBootstrapFailedAtUtc: null,
                PopulationBootstrapFailureCode: null,
                EconomyBootstrapFailureCode: null,
                ProvisioningStartedAtUtc: now.AddMinutes(-30),
                ProvisioningHeartbeatAtUtc: now.AddMinutes(-5),
                ProvisioningLeaseExpiresAtUtc: now.AddMinutes(10),
                ProvisioningAttemptCount: 2,
                ArchivedAtUtc: null,
                IsArchived: false);
        }

        public static CityProvisioningModel CreateProvisioningModel(Guid? cityId = null)
        {
            Guid effectiveCityId = cityId ?? Guid.Parse("71b8b30b-d43c-4111-b1da-02c4d258cb18");

            return new CityProvisioningModel(
                CityId: effectiveCityId,
                SimulationKind: "ClassicCity",
                PopulationBootstrap: new CityPopulationBootstrapModel(
                    OperationId: Guid.Parse("f91e16fd-ee76-4330-8dda-3fb5b8749d52"),
                    Status: "Completed",
                    PlannedPeopleCount: 120,
                    ResidentialCapacity: 112,
                    Summary: new CityPopulationBootstrapSummaryModel(
                        CityId: effectiveCityId,
                        RequestedPeopleCount: 120,
                        GeneratedPeopleCount: 118,
                        HouseholdCount: 42,
                        HousedHouseholdCount: 39,
                        HomelessHouseholdCount: 3,
                        HousedPeopleCount: 112,
                        HomelessPeopleCount: 6),
                    FailureCode: null),
                EconomyBootstrap: new CityEconomyBootstrapModel(
                    OperationId: Guid.Parse("2c03f709-08b6-4d6c-8789-f45f5ecdd3a3"),
                    Status: "Completed",
                    FailureCode: null,
                    UnitKind: "Currency",
                    UnitCode: "CRD",
                    UnitDisplayName: "Credits",
                    UnitSymbol: "C"));
        }

        public static CityGenerationCatalogDto CreateGenerationCatalogDto()
        {
            return new CityGenerationCatalogDto(
                CityNamePresets:
                [
                    "Mega City",
                    "Neo Harbor"
                ],
                DistrictNamePresets:
                [
                    "North",
                    "South"
                ],
                StreetNamePresets:
                [
                    "Central Avenue",
                    "Orbital Road"
                ]);
        }

        public static SuggestedCityNamesDto CreateSuggestedCityNamesDto(string? seed = "seed-42")
        {
            return new SuggestedCityNamesDto(
                Seed: seed,
                Names:
                [
                    "Mega City",
                    "Neo Harbor",
                    "Copper Hill"
                ]);
        }

        public static DistrictDto CreateDistrictDto(Guid cityId)
        {
            return new DistrictDto(
                DistrictId: Guid.Parse("2be5fd6f-c7a4-41b0-87da-73e7048919a8"),
                CityId: cityId,
                Name: "North District",
                AnchorX: 12.5m,
                AnchorY: 42.75m,
                CreatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }

        public static RoadNodeDto CreateRoadNodeDto(
            Guid cityId,
            Guid districtId)
        {
            return new RoadNodeDto(
                RoadNodeId: Guid.Parse("715942c3-f84a-49cc-a96c-0b344a98f9c2"),
                CityId: cityId,
                DistrictId: districtId,
                Name: "North Hub",
                Type: "Intersection",
                PositionX: 13.0m,
                PositionY: 41.0m,
                CreatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 8,
                    minute: 5,
                    second: 0,
                    offset: TimeSpan.Zero));
        }

        public static ResidentialBuildingDto CreateResidentialBuildingDto(
            Guid cityId,
            Guid districtId,
            Guid accessRoadNodeId)
        {
            return new ResidentialBuildingDto(
                ResidentialBuildingId: Guid.Parse("d648425f-7d2d-4eea-aa40-1af88e7d7d6f"),
                CityId: cityId,
                DistrictId: districtId,
                AccessRoadNodeId: accessRoadNodeId,
                Name: "North Tower",
                Type: "ApartmentBlock",
                ResidentCapacity: 120,
                PositionX: 14.0m,
                PositionY: 39.5m,
                CreatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 8,
                    minute: 10,
                    second: 0,
                    offset: TimeSpan.Zero));
        }

        public static CityAnchorDto CreateCityAnchorDto(
            Guid cityId,
            Guid districtId,
            Guid accessRoadNodeId)
        {
            return new CityAnchorDto(
                CityAnchorId: Guid.Parse("b0e3872e-1524-465a-b941-0e0d1cf0630c"),
                CityId: cityId,
                DistrictId: districtId,
                AccessRoadNodeId: accessRoadNodeId,
                Name: "North Hospital",
                Type: "Healthcare",
                Capacity: 80,
                PositionX: 15.0m,
                PositionY: 38.0m,
                CreatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 8,
                    minute: 15,
                    second: 0,
                    offset: TimeSpan.Zero));
        }

        public static RoadSegmentDto CreateRoadSegmentDto(
            Guid cityId,
            Guid districtId,
            Guid fromRoadNodeId,
            Guid toRoadNodeId)
        {
            return new RoadSegmentDto(
                RoadSegmentId: Guid.Parse("99fd54d6-9a4b-4f35-8d74-b75ac4163bff"),
                CityId: cityId,
                DistrictId: districtId,
                FromRoadNodeId: fromRoadNodeId,
                ToRoadNodeId: toRoadNodeId,
                Name: "North Connector",
                Type: "LocalRoad",
                LengthMeters: 240m,
                CreatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 8,
                    minute: 20,
                    second: 0,
                    offset: TimeSpan.Zero));
        }

        public static CityMapTopologyDto CreateMapTopologyDto(Guid cityId)
        {
            DistrictDto district = CreateDistrictDto(cityId);
            RoadNodeDto roadNode = CreateRoadNodeDto(
                cityId: cityId,
                districtId: district.DistrictId);
            ResidentialBuildingDto building = CreateResidentialBuildingDto(
                cityId: cityId,
                districtId: district.DistrictId,
                accessRoadNodeId: roadNode.RoadNodeId);
            CityAnchorDto anchor = CreateCityAnchorDto(
                cityId: cityId,
                districtId: district.DistrictId,
                accessRoadNodeId: roadNode.RoadNodeId);
            RoadSegmentDto segment = CreateRoadSegmentDto(
                cityId: cityId,
                districtId: district.DistrictId,
                fromRoadNodeId: roadNode.RoadNodeId,
                toRoadNodeId: roadNode.RoadNodeId);

            return new CityMapTopologyDto(
                CityId: cityId,
                Districts: [district],
                ResidentialBuildings: [building],
                Anchors: [anchor],
                RoadNodes: [roadNode],
                RoadSegments: [segment]);
        }

        public static CityRoadGraphDto CreateRoadGraphDto(Guid cityId)
        {
            DistrictDto district = CreateDistrictDto(cityId);
            RoadNodeDto roadNode = CreateRoadNodeDto(
                cityId: cityId,
                districtId: district.DistrictId);
            RoadSegmentDto segment = CreateRoadSegmentDto(
                cityId: cityId,
                districtId: district.DistrictId,
                fromRoadNodeId: roadNode.RoadNodeId,
                toRoadNodeId: roadNode.RoadNodeId);

            return new CityRoadGraphDto(
                CityId: cityId,
                Districts: [district],
                RoadSegments: [segment]);
        }

        public static CityWeatherDto CreateWeatherDto(Guid cityId)
        {
            return new CityWeatherDto(
                CityId: cityId,
                ClimateZone: "Continental",
                Hemisphere: "Northern",
                UtcOffsetMinutes: 180,
                CurrentType: "Clear",
                Severity: "None",
                PrecipitationKind: "None",
                TemperatureC: 18.5m,
                HumidityPercent: 45m,
                WindSpeedKph: 10m,
                CloudCoveragePercent: 15m,
                PressureHpa: 1017m,
                StartedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 9,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                ExpectedUntilUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 15,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                LastEvaluatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 9,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero),
                LastTransitionAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 8,
                    minute: 45,
                    second: 0,
                    offset: TimeSpan.Zero),
                ActiveOverride: null);
        }

        public static ResolveCityRouteRequest CreateResolveCityRouteRequest(
            Guid fromId,
            Guid toId)
        {
            return new ResolveCityRouteRequest(
                From: new CityRoutePointRequest(
                    Kind: "Anchor",
                    Id: fromId),
                To: new CityRoutePointRequest(
                    Kind: "Building",
                    Id: toId),
                Profile: "Pedestrian");
        }

        public static CityRouteDto CreateRouteDto(Guid cityId)
        {
            var districtId = Guid.Parse("2be5fd6f-c7a4-41b0-87da-73e7048919a8");
            var roadNodeId = Guid.Parse("715942c3-f84a-49cc-a96c-0b344a98f9c2");

            return new CityRouteDto(
                CityId: cityId,
                Profile: "Pedestrian",
                Accessible: true,
                UsedDynamicRoadConditions: true,
                EffectiveTickId: 42,
                ConditionsLastEvaluatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 9,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero),
                From: new CityRoutePointDto(
                    Kind: "Anchor",
                    EntityId: Guid.Parse("b0e3872e-1524-465a-b941-0e0d1cf0630c"),
                    DistrictId: districtId,
                    RoadNodeId: roadNodeId,
                    Name: "North Hospital",
                    PositionX: 15m,
                    PositionY: 38m),
                To: new CityRoutePointDto(
                    Kind: "Building",
                    EntityId: Guid.Parse("d648425f-7d2d-4eea-aa40-1af88e7d7d6f"),
                    DistrictId: districtId,
                    RoadNodeId: roadNodeId,
                    Name: "North Tower",
                    PositionX: 14m,
                    PositionY: 39.5m),
                TotalDistanceMeters: 240m,
                EstimatedTravelTimeMinutes: 4m,
                OverallPassabilityIndex: 0.92m,
                UnreachableReason: null,
                Segments:
                [
                    new CityRouteSegmentDto(
                        RoadSegmentId: Guid.Parse("99fd54d6-9a4b-4f35-8d74-b75ac4163bff"),
                        DistrictId: districtId,
                        FromRoadNodeId: roadNodeId,
                        ToRoadNodeId: roadNodeId,
                        Name: "North Connector",
                        Type: "LocalRoad",
                        LengthMeters: 240m,
                        EstimatedTraversalMinutes: 4m,
                        PassabilityIndex: 0.92m,
                        SpeedMultiplierIndex: 0.98m,
                        SlipRiskIndex: 0.1m,
                        ClosureRiskIndex: 0.03m)
                ]);
        }

        public static DispatchCityTripRequest CreateDispatchCityTripRequest(
            Guid fromId,
            Guid toId)
        {
            return new DispatchCityTripRequest(
                From: new CityRoutePointRequest(
                    Kind: "Anchor",
                    Id: fromId),
                To: new CityRoutePointRequest(
                    Kind: "Building",
                    Id: toId),
                Purpose: "WorkCommute",
                Profile: "Pedestrian",
                MovementCapabilityIndex: 0.85m,
                TravellerEntityId: Guid.Parse("249d15cc-b3a4-4151-898d-8dc96253a95b"),
                Subject: "Commuter");
        }

        public static CityActiveTripDto CreateActiveTripDto(Guid cityId)
        {
            var districtId = Guid.Parse("2be5fd6f-c7a4-41b0-87da-73e7048919a8");
            var roadNodeId = Guid.Parse("715942c3-f84a-49cc-a96c-0b344a98f9c2");
            var roadSegmentId = Guid.Parse("99fd54d6-9a4b-4f35-8d74-b75ac4163bff");

            return new CityActiveTripDto(
                TripId: Guid.Parse("6bf3c40d-f6dc-49ce-9957-0d3877926d36"),
                CityId: cityId,
                TravellerEntityId: Guid.Parse("249d15cc-b3a4-4151-898d-8dc96253a95b"),
                Subject: "Commuter",
                Purpose: "WorkCommute",
                Profile: "Pedestrian",
                Status: "Active",
                MovementCapabilityIndex: 0.85m,
                UsedDynamicRoadConditions: true,
                PlannedAtTickId: 42,
                ConditionsEffectiveTickId: 42,
                LastAdvancedTickId: 43,
                StartedAtSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 9,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero),
                LastAdvancedAtSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 9,
                    minute: 32,
                    second: 0,
                    offset: TimeSpan.Zero),
                ExpectedArrivalAtSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 9,
                    minute: 36,
                    second: 0,
                    offset: TimeSpan.Zero),
                ArrivedAtSimTimeUtc: null,
                CurrentProgressIndex: 0.4m,
                TotalDistanceMeters: 240m,
                DistanceTravelledMeters: 96m,
                RemainingDistanceMeters: 144m,
                PlannedTravelTimeMinutes: 4m,
                AdjustedTravelTimeMinutes: 6m,
                From: new CityActiveTripEndpointDto(
                    Kind: "Anchor",
                    EntityId: Guid.Parse("b0e3872e-1524-465a-b941-0e0d1cf0630c"),
                    DistrictId: districtId,
                    RoadNodeId: roadNodeId,
                    Name: "North Hospital",
                    PositionX: 15m,
                    PositionY: 38m),
                To: new CityActiveTripEndpointDto(
                    Kind: "Building",
                    EntityId: Guid.Parse("d648425f-7d2d-4eea-aa40-1af88e7d7d6f"),
                    DistrictId: districtId,
                    RoadNodeId: roadNodeId,
                    Name: "North Tower",
                    PositionX: 14m,
                    PositionY: 39.5m),
                Current: new CityActiveTripProgressDto(
                    DistrictId: districtId,
                    RoadSegmentId: roadSegmentId,
                    SegmentProgressIndex: 0.4m,
                    PositionX: 14.6m,
                    PositionY: 38.6m));
        }

        public static WebApplicationBuilder CreateBuilder(IConfiguration? configuration = null)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(
                new WebApplicationOptions
                {
                    EnvironmentName = "Development"
                });

            if (configuration is not null)
            {
                builder.Configuration.Sources.Clear();
                builder.Configuration.AddConfiguration(configuration);
            }

            return builder;
        }

        public static IConfiguration BuildValidApiConfiguration()
        {
            Dictionary<string, string?> values = new()
            {
                ["ConnectionStrings:SimulationCoreDb"] =
                    "Host=localhost;Port=5432;Database=simulationcore_tests;Username=postgres;Password=postgres",
                ["InternalUserContextJwt:Issuer"] = "https://gateway.test",
                ["InternalUserContextJwt:Audience"] = "simulationcore-api",
                ["InternalUserContextJwt:SigningKey"] = "0123456789abcdef0123456789abcdef",
                ["InternalUserContextJwt:LifetimeSeconds"] = "300",
                ["InternalServiceJwt:Issuer"] = "https://gateway.test",
                ["InternalServiceJwt:Audience"] = "simulationcore-api",
                ["InternalServiceJwt:SigningKey"] = "abcdef0123456789abcdef0123456789",
                ["InternalServiceJwt:LifetimeSeconds"] = "300",
                ["RabbitMq:Host"] = "rabbitmq.test",
                ["RabbitMq:Port"] = "5672",
                ["RabbitMq:VirtualHost"] = "/",
                ["RabbitMq:Username"] = "guest",
                ["RabbitMq:Password"] = "guest",
                ["RabbitMq:EndpointHygiene:DiscardSkippedMessages"] = "true",
                ["DownstreamServices:Economy"] = "https://economy.test",
                ["DownstreamServices:Population"] = "https://population.test",
                ["DownstreamServices:SimulationSystems"] = "https://simulationsystems.test",
                ["DatabaseStartup:Enabled"] = "false"
            };

            return new ConfigurationBuilder()
               .AddInMemoryCollection(values)
               .Build();
        }

        public static T AssertResult<T>(
            IResult result,
            int expectedStatusCode)
        {
            IStatusCodeHttpResult status = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
            Assert.Equal(
                expected: expectedStatusCode,
                actual: status.StatusCode);

            IValueHttpResult value = Assert.IsAssignableFrom<IValueHttpResult>(result);
            return Assert.IsType<T>(value.Value);
        }

        public static void AssertStatus(
            IResult result,
            int expectedStatusCode)
        {
            IStatusCodeHttpResult status = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
            Assert.Equal(
                expected: expectedStatusCode,
                actual: status.StatusCode);
        }

        public static TProperty GetAnonymousProperty<TProperty>(
            IResult result,
            string propertyName,
            int expectedStatusCode)
        {
            IStatusCodeHttpResult status = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
            Assert.Equal(
                expected: expectedStatusCode,
                actual: status.StatusCode);

            IValueHttpResult valueResult = Assert.IsAssignableFrom<IValueHttpResult>(result);
            object? value = valueResult.Value;
            Assert.NotNull(value);
            object? propertyValue = value.GetType()
               .GetProperty(propertyName)
              ?.GetValue(value);
            Assert.NotNull(propertyValue);
            return Assert.IsType<TProperty>(propertyValue);
        }

        public sealed class FakeSender : IMediator
        {
            private readonly Dictionary<Type, Func<object, CancellationToken, Task<object?>>> _handlers = new();

            public List<object> Requests { get; } = [];

            public Task<TResponse> Send<TResponse>(
                IRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                Requests.Add(request);

                if (!_handlers.TryGetValue(
                        key: request.GetType(),
                        value: out Func<object, CancellationToken, Task<object?>>? handler))
                    throw new InvalidOperationException(
                        $"No handler registered for request type '{request.GetType().Name}'.");

                return Invoke<TResponse>(
                    handler: handler,
                    request: request,
                    cancellationToken: cancellationToken);
            }

            public async Task Send<TRequest>(
                TRequest request,
                CancellationToken cancellationToken = default)
                where TRequest : IRequest
            {
                Requests.Add(request);

                if (!_handlers.TryGetValue(
                        key: request.GetType(),
                        value: out Func<object, CancellationToken, Task<object?>>? handler))
                    throw new InvalidOperationException(
                        $"No handler registered for request type '{request.GetType().Name}'.");

                await handler(
                    arg1: request,
                    arg2: cancellationToken);
            }

            public async Task<object?> Send(
                object request,
                CancellationToken cancellationToken = default)
            {
                Requests.Add(request);

                if (!_handlers.TryGetValue(
                        key: request.GetType(),
                        value: out Func<object, CancellationToken, Task<object?>>? handler))
                    throw new InvalidOperationException(
                        $"No handler registered for request type '{request.GetType().Name}'.");

                return await handler(
                    arg1: request,
                    arg2: cancellationToken);
            }

            public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
                IStreamRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public IAsyncEnumerable<object?> CreateStream(
                object request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task Publish(
                object notification,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task Publish<TNotification>(
                TNotification notification,
                CancellationToken cancellationToken = default)
                where TNotification : INotification
            {
                return Task.CompletedTask;
            }

            public void Handle<TRequest, TResponse>(Func<TRequest, TResponse> handler)
                where TRequest : notnull
            {
                _handlers[typeof(TRequest)] = (
                    request,
                    _) => Task.FromResult<object?>(handler((TRequest)request));
            }

            public void Handle<TRequest>(Action<TRequest> handler)
                where TRequest : notnull
            {
                _handlers[typeof(TRequest)] = (
                    request,
                    _) =>
                {
                    handler((TRequest)request);
                    return Task.FromResult<object?>(Unit.Value);
                };
            }

            private static async Task<TResponse> Invoke<TResponse>(
                Func<object, CancellationToken, Task<object?>> handler,
                object request,
                CancellationToken cancellationToken)
            {
                object? result = await handler(
                    arg1: request,
                    arg2: cancellationToken);
                return (TResponse)result!;
            }
        }
    }
}
