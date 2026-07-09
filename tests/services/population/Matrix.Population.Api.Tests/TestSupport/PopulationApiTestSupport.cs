using Matrix.Population.Contracts.Models;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Matrix.Population.Api.Tests.TestSupport
{
    public static class PopulationApiTestSupport
    {
        public static PersonDto CreatePersonDto(
            Guid? id = null,
            string fullName = "Thomas Anderson")
        {
            return new PersonDto(
                Id: id ?? Guid.Parse("544ed8cb-03e9-48d7-91a3-6d4dd3047203"),
                FullName: fullName,
                Sex: "Male",
                BirthDate: "1990-03-11",
                DeathDate: null,
                Age: 34,
                AgeGroup: "Adult",
                LifeStatus: "Alive",
                MaritalStatus: "Single",
                EducationLevel: "Higher",
                Health: 82,
                Happiness: 71,
                Energy: 68,
                Stress: 29,
                SocialNeed: 41,
                EmploymentStatus: "Employed",
                JobTitle: "Operator");
        }

        public static CityResidentDetailsDto CreateResidentDetailsDto(
            Guid? id = null,
            string fullName = "Thomas Anderson")
        {
            return new CityResidentDetailsDto(
                Id: id ?? Guid.Parse("544ed8cb-03e9-48d7-91a3-6d4dd3047203"),
                FullName: fullName,
                Sex: "Male",
                BirthDate: "1990-03-11",
                DeathDate: null,
                Age: 34,
                AgeGroup: "Adult",
                LifeStatus: "Alive",
                MaritalStatus: "Single",
                EducationLevel: "Higher",
                Health: 82,
                Happiness: 71,
                Energy: 68,
                Stress: 29,
                SocialNeed: 41,
                EmploymentStatus: "Employed",
                JobTitle: "Operator",
                CurrentSpouse: null,
                Mother: null,
                Father: null,
                Children: [],
                LastChildbirthDate: null,
                CurrentHousing: new CityResidentHousingDto(
                    HouseholdId: Guid.Parse("922eb4d0-6704-49c9-a39c-2de460ed060e"),
                    HousingStatus: "Housed",
                    ResidentialBuildingId: Guid.Parse("f8ee3fd5-9df3-4efa-930e-016fdb53d9d3")),
                CurrentWorkplace: new CityResidentWorkplaceDto(
                    WorkplaceId: Guid.Parse("1fca9b68-b5b8-4f5d-baeb-fe9dd664ff93"),
                    WorkplaceAnchorId: Guid.Parse("daa7773e-fdfc-462f-a9b8-19605d6a113e"),
                    RouteAccess: new CityResidentRouteAccessDto(
                        HasRouteData: true,
                        IsAccessible: true,
                        AccessibilityIndex: 0.91m,
                        PassabilityIndex: 0.87m,
                        EstimatedTravelTimeMinutes: 18m)),
                CurrentEducationInstitution: null,
                PrimaryHealthcareProvider: null,
                CurrentActiveTrip: null);
        }

        public static CityPopulationSummaryDto CreateCityPopulationSummaryDto(Guid cityId)
        {
            return new CityPopulationSummaryDto(
                CityId: cityId,
                CurrentDate: "2048-06-01",
                Lifecycle: new CityPopulationSummaryLifecycleDto(
                    IsArchived: false,
                    ArchivedAtUtc: null,
                    IsDeleted: false,
                    DeletedAtUtc: null),
                Environment: new CityPopulationSummaryEnvironmentDto(
                    ClimateZone: "Continental",
                    Hemisphere: "Northern",
                    UtcOffsetMinutes: 180,
                    UpdatedAtUtc: "2048-06-01T10:00:00Z"),
                Simulation: new CityPopulationSummarySimulationDto(
                    LastProcessedTickId: 42,
                    LastProcessedDate: "2048-06-01",
                    UpdatedAtUtc: "2048-06-01T10:00:00Z"),
                Weather: new CityPopulationSummaryWeatherDto(
                    CurrentType: "Clear",
                    CurrentSeverity: "None",
                    IsRecoveryActive: false,
                    CurrentWeatherEffectiveAtSimTimeUtc: "2048-06-01T09:00:00Z",
                    LastWeatherOccurredOnUtc: "2048-05-31T22:00:00Z",
                    LastExposureProcessedAtSimTimeUtc: "2048-06-01T09:30:00Z",
                    LastWeatherImpactAppliedAtSimTimeUtc: null),
                Housing: new CityPopulationSummaryHousingDto(
                    HouseholdCount: 20,
                    HousedHouseholdCount: 18,
                    HomelessHouseholdCount: 2),
                Residents: new CityPopulationSummaryResidentsDto(
                    ResidentCount: 55,
                    DeceasedCount: 1,
                    HousedResidentCount: 50,
                    HomelessResidentCount: 5,
                    ChildCount: 8,
                    YouthCount: 6,
                    AdultCount: 33,
                    SeniorCount: 8,
                    EmployedCount: 24,
                    StudentCount: 10,
                    UnemployedCount: 13,
                    RetiredCount: 8,
                    AverageHealth: 78.4m,
                    AverageHappiness: 69.2m,
                    AverageEnergy: 72.1m,
                    AverageStress: 31.7m,
                    AverageSocialNeed: 40.5m,
                    ActiveIllnessCount: 4,
                    SevereIllnessCount: 1,
                    MedicalLoadIndex: 0.22m,
                    TriagePressureIndex: 0.11m,
                    RecoverySupportIndex: 0.75m,
                    WorkforceCommuteAccessibilityIndex: 0.83m,
                    WorkforceAttendanceIndex: 0.89m,
                    WorkforceProductivityIndex: 0.81m,
                    StudentCommuteAccessibilityIndex: 0.78m,
                    StudentAttendanceIndex: 0.85m));
        }

        public static CityPopulationDashboardDto CreateDashboardDto(Guid cityId)
        {
            return new CityPopulationDashboardDto(
                CityId: cityId,
                CurrentDate: "2048-06-01",
                GeneratedAtUtc: "2048-06-01T10:05:00Z",
                Metrics:
                [
                    new CityPopulationDashboardMetricDto(
                        Key: "population",
                        Label: "Population",
                        Description: "Total residents",
                        ValueKind: "count",
                        CurrentValue: 55,
                        DeltaYesterday: 1,
                        DeltaMonth: 4,
                        DeltaYear: 12)
                ],
                RecentEvents:
                [
                    new CityPopulationActivityEventDto(
                        ActivityEventId: Guid.Parse("7d2b5a00-8e3b-46c7-8f2d-21fa5d2e4601"),
                        CurrentDate: "2048-06-01",
                        OccurredAtUtc: "2048-06-01T09:55:00Z",
                        EventType: "Birth",
                        Source: "CivilRegistry",
                        Severity: "Info",
                        Title: "Newborn registered",
                        Summary: "A new resident was added.",
                        PrimaryResidentId: Guid.Parse("a1571b54-8bbc-4dfd-85e1-2c62a16c50ad"),
                        SecondaryResidentId: null)
                ]);
        }

        public static CityPopulationDistrictPressureDto CreateDistrictPressureDto(Guid cityId)
        {
            return new CityPopulationDistrictPressureDto(
                CityId: cityId,
                GeneratedAtUtc: "2048-06-01T10:10:00Z",
                Districts:
                [
                    new CityPopulationDistrictPressureItemDto(
                        DistrictId: Guid.Parse("3d360bb6-1225-4405-93b7-cf486f7a5f42"),
                        ResidentCount: 30,
                        HouseholdCount: 12,
                        HomelessResidentCount: 2,
                        AverageHealth: 79.2m,
                        AverageStress: 28.3m,
                        AverageHappiness: 70.1m,
                        ActiveIllnessCount: 2,
                        SevereIllnessCount: 1,
                        UtilityContinuityIndex: 0.91m,
                        UtilityIncidentPressureIndex: 0.18m,
                        HousingFragilityIndex: 0.22m,
                        PopulationPressureIndex: 0.34m)
                ]);
        }

        public static CityEmploymentCatalogDto CreateEmploymentCatalogDto()
        {
            return new CityEmploymentCatalogDto(
                JobTitles:
                [
                    "Operator",
                    "Medic"
                ],
                CurrentWorkplaces:
                [
                    new CityEmploymentWorkplaceDto(
                        WorkplaceId: Guid.Parse("b69348d2-c4b3-4fdc-97e0-48902d5b3098"),
                        WorkplaceAnchorId: Guid.Parse("2fd283ef-6f6d-4c1b-8f46-0d1d2caab431"),
                        JobTitle: "Operator",
                        ResidentCount: 5)
                ]);
        }

        public static CityEducationCatalogDto CreateEducationCatalogDto()
        {
            return new CityEducationCatalogDto(
                CurrentInstitutions:
                [
                    new CityEducationInstitutionDto(
                        InstitutionId: Guid.Parse("0df72f31-a3a9-4317-9f7f-078d1814eb95"),
                        InstitutionAnchorId: Guid.Parse("455918de-4795-4960-a1e8-cd1818c60168"),
                        EducationLevel: "Higher",
                        ResidentCount: 11)
                ]);
        }

        public static CityPopulationBootstrapSummaryDto CreateBootstrapSummaryDto(Guid cityId)
        {
            return new CityPopulationBootstrapSummaryDto(
                CityId: cityId,
                RequestedPeopleCount: 60,
                GeneratedPeopleCount: 60,
                HouseholdCount: 22,
                HousedHouseholdCount: 20,
                HomelessHouseholdCount: 2,
                HousedPeopleCount: 55,
                HomelessPeopleCount: 5);
        }

        public static CityEmploymentOperationResultDto CreateEmploymentOperationResultDto(string action = "Hire")
        {
            return new CityEmploymentOperationResultDto(
                Action: action,
                RecordedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 10,
                    minute: 30,
                    second: 0,
                    offset: TimeSpan.Zero),
                Resident: CreateResidentDetailsDto());
        }

        public static CityEducationOperationResultDto CreateEducationOperationResultDto(string action = "Enroll")
        {
            return new CityEducationOperationResultDto(
                Action: action,
                RecordedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 10,
                    minute: 45,
                    second: 0,
                    offset: TimeSpan.Zero),
                Resident: CreateResidentDetailsDto());
        }

        public static CityCivilRegistryOperationResultDto CreateCivilRegistryOperationResultDto(
            string action = "Marriage")
        {
            return new CityCivilRegistryOperationResultDto(
                Action: action,
                RecordedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 11,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                FirstResident: CreateResidentDetailsDto(
                    id: Guid.Parse("544ed8cb-03e9-48d7-91a3-6d4dd3047203"),
                    fullName: "Thomas Anderson"),
                SecondResident: CreateResidentDetailsDto(
                    id: Guid.Parse("c2ec2f2d-0089-424c-bd7e-b454d8f46f2f"),
                    fullName: "Trinity"));
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
                ["ConnectionStrings:PopulationDb"] =
                    "Host=localhost;Port=5432;Database=population_tests;Username=postgres;Password=postgres",
                ["InternalUserContextJwt:Issuer"] = "https://gateway.test",
                ["InternalUserContextJwt:Audience"] = "population-api",
                ["InternalUserContextJwt:SigningKey"] = "0123456789abcdef0123456789abcdef",
                ["InternalUserContextJwt:LifetimeSeconds"] = "300",
                ["InternalServiceJwt:Issuer"] = "https://gateway.test",
                ["InternalServiceJwt:Audience"] = "population-api",
                ["InternalServiceJwt:SigningKey"] = "abcdef0123456789abcdef0123456789",
                ["InternalServiceJwt:LifetimeSeconds"] = "300",
                ["RabbitMq:Host"] = "rabbitmq.test",
                ["RabbitMq:Port"] = "5672",
                ["RabbitMq:VirtualHost"] = "/",
                ["RabbitMq:Username"] = "guest",
                ["RabbitMq:Password"] = "guest",
                ["DownstreamServices:SimulationCore"] = "https://simulationcore.test",
                ["DownstreamServices:SimulationSystems"] = "https://simulationsystems.test",
                ["ProcessedIntegrationMessageCleanup:PollIntervalSeconds"] = "300",
                ["ProcessedIntegrationMessageCleanup:BatchSize"] = "100",
                ["RabbitMq:EndpointHygiene:DiscardSkippedMessages"] = "true",
                ["DatabaseStartup:Enabled"] = "false"
            };

            return new ConfigurationBuilder()
               .AddInMemoryCollection(values)
               .Build();
        }

        public sealed class FakeSender : ISender
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
