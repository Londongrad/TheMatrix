using System.Net;
using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.ApiGateway.Controllers.SimulationCore.Scenarios.ClassicCity.Cities;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Education.Contracts.Enrollments;
using Matrix.Education.Contracts.Institutions;
using Matrix.Education.Contracts.Students;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Requests;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Views;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Cities.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Heating.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.PowerDistribution.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.Sanitation.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Requests;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.UtilityIncidents.Views;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.WaterDistribution.Views;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Controllers.SimulationCore.Scenarios.ClassicCity
{
    public sealed class CitiesControllerTests
    {
        [Fact]
        public void Route_IsScopedToClassicCityScenario()
        {
            RouteAttribute route = Assert.Single(
                typeof(CitiesController).GetCustomAttributes(
                    attributeType: typeof(RouteAttribute),
                    inherit: true)
                   .Cast<RouteAttribute>());

            Assert.Equal(
                expected: "api/scenarios/classic-city/cities",
                actual: route.Template);
        }

        [Fact]
        public async Task Create_ReturnsScenarioScopedLocation()
        {
            CitiesController controller = CreateCitiesController();

            ActionResult<CityProvisioningView> actionResult = await controller.Create(
                request: CreateCityLaunchRequest(),
                cancellationToken: CancellationToken.None);

            CreatedResult created = Assert.IsType<CreatedResult>(actionResult.Result);
            CityProvisioningView view = Assert.IsType<CityProvisioningView>(created.Value);
            Assert.Equal(
                expected: $"/api/scenarios/classic-city/cities/{view.CityId}",
                actual: created.Location);
        }

        [Fact]
        public async Task GetDashboard_WhenEconomySummaryIsAvailable_AppendsEconomyMetrics()
        {
            var cityId = Guid.Parse("1a87a441-fdb8-4224-9d27-7f48d4f41516");
            CityPopulationDashboardDto dashboard = CreateCityPopulationDashboardDto(cityId);
            var populationClient = new RecordingPopulationApiClient
            {
                DashboardResult = dashboard
            };
            var economyClient = new RecordingEconomyApiClient
            {
                CitySummaryResult = CreateEconomySummaryView()
            };
            CitiesController controller = CreateCitiesController(
                populationClient: populationClient,
                economyClient: economyClient);

            ActionResult<CityPopulationDashboardDto> actionResult = await controller.GetDashboard(
                cityId: cityId,
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            CityPopulationDashboardDto view = Assert.IsType<CityPopulationDashboardDto>(ok.Value);
            Assert.Equal(
                expected: cityId,
                actual: populationClient.LastDashboardCityId);
            Assert.Equal(
                expected: cityId,
                actual: economyClient.LastCitySummaryCityId);
            Assert.Equal(
                expected: dashboard.Metrics.Count + 6,
                actual: view.Metrics.Count);
            Assert.Contains(
                collection: view.Metrics,
                filter: x => x.Key == "economyBudgetBalance");
            Assert.Contains(
                collection: view.Metrics,
                filter: x => x.Key == "economyGrossPayroll");
            Assert.Contains(
                collection: view.Metrics,
                filter: x => x.Key == "economyIncomeTax");
            Assert.Contains(
                collection: view.Metrics,
                filter: x => x.Key == "economySalesTax");
            Assert.Contains(
                collection: view.Metrics,
                filter: x => x.Key == "economyRetailTurnover");
            Assert.Contains(
                collection: view.Metrics,
                filter: x => x.Key == "economyCityExpenses");
            Assert.Equal(
                expected: dashboard.RecentEvents,
                actual: view.RecentEvents);
        }

        [Fact]
        public async Task GetDashboard_WhenEconomyFails_ReturnsPopulationDashboardOnly()
        {
            var cityId = Guid.Parse("31893803-e632-4cd7-b08d-8ff784c79206");
            CityPopulationDashboardDto dashboard = CreateCityPopulationDashboardDto(cityId);
            var populationClient = new RecordingPopulationApiClient
            {
                DashboardResult = dashboard
            };
            var economyClient = new RecordingEconomyApiClient
            {
                GetCitySummaryException = CreateDownstreamServiceException(
                    statusCode: HttpStatusCode.BadGateway,
                    serviceName: "economy")
            };
            CitiesController controller = CreateCitiesController(
                populationClient: populationClient,
                economyClient: economyClient);

            ActionResult<CityPopulationDashboardDto> actionResult = await controller.GetDashboard(
                cityId: cityId,
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            CityPopulationDashboardDto view = Assert.IsType<CityPopulationDashboardDto>(ok.Value);
            Assert.Equal(
                expected: dashboard.Metrics,
                actual: view.Metrics);
            Assert.Equal(
                expected: dashboard.RecentEvents,
                actual: view.RecentEvents);
        }

        [Fact]
        public async Task GetDistrictInfrastructure_WhenAnySliceIsMissing_ReturnsNotFound()
        {
            var cityId = Guid.Parse("e6bd2dfc-a93f-4aea-abdf-556308c2a0f4");
            var environmentalConditionsClient = new RecordingEnvironmentalConditionsApiClient
            {
                ReturnSanitationNull = true
            };
            CitiesController controller =
                CreateCitiesController(environmentalConditionsClient: environmentalConditionsClient);

            ActionResult<CityDistrictInfrastructureView> actionResult = await controller.GetDistrictInfrastructure(
                cityId: cityId,
                cancellationToken: CancellationToken.None);

            Assert.IsType<NotFoundResult>(actionResult.Result);
        }

        [Fact]
        public async Task GetDistrictInfrastructure_WhenAllSlicesExist_ReturnsCombinedView()
        {
            var cityId = Guid.Parse("b177e44d-c0ae-4e18-ae27-c4f28fc73fb9");
            CityDistrictHeatingConditionsView heating = CreateCityDistrictHeatingConditionsView(cityId);
            CityDistrictWaterDistributionConditionsView water =
                CreateCityDistrictWaterDistributionConditionsView(cityId);
            CityDistrictPowerDistributionConditionsView power =
                CreateCityDistrictPowerDistributionConditionsView(cityId);
            CityDistrictSanitationConditionsView sanitation = CreateCityDistrictSanitationConditionsView(cityId);
            CityDistrictUtilityIncidentConditionsView utilityIncidents =
                CreateCityDistrictUtilityIncidentConditionsView(cityId);
            var environmentalConditionsClient = new RecordingEnvironmentalConditionsApiClient
            {
                HeatingResult = heating,
                WaterResult = water,
                PowerResult = power,
                SanitationResult = sanitation,
                UtilityIncidentResult = utilityIncidents
            };
            DateTimeOffset generatedAtUtc = new(
                year: 2048,
                month: 6,
                day: 3,
                hour: 14,
                minute: 0,
                second: 0,
                offset: TimeSpan.Zero);
            CitiesController controller = CreateCitiesController(
                environmentalConditionsClient: environmentalConditionsClient,
                timeProvider: CreateTimeProvider(generatedAtUtc));

            ActionResult<CityDistrictInfrastructureView> actionResult = await controller.GetDistrictInfrastructure(
                cityId: cityId,
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            CityDistrictInfrastructureView view = Assert.IsType<CityDistrictInfrastructureView>(ok.Value);
            Assert.Equal(
                expected: cityId,
                actual: view.CityId);
            Assert.Same(
                expected: heating,
                actual: view.Heating);
            Assert.Same(
                expected: water,
                actual: view.WaterDistribution);
            Assert.Same(
                expected: power,
                actual: view.PowerDistribution);
            Assert.Same(
                expected: sanitation,
                actual: view.Sanitation);
            Assert.Same(
                expected: utilityIncidents,
                actual: view.UtilityIncidents);
            Assert.Equal(
                expected: generatedAtUtc,
                actual: view.GeneratedAtUtc);
        }

        [Fact]
        public async Task DispatchDistrictUtilityResponse_WhenDownstreamReturnsConflict_MapsConflictBody()
        {
            var cityId = Guid.Parse("c7a3da3e-2671-405c-a049-f4fead610ab0");
            CityUtilityIncidentStatusView conflictView = CreateCityUtilityIncidentStatusView(
                cityId: cityId,
                statusIntensity: "Critical");
            var environmentalConditionsClient = new RecordingEnvironmentalConditionsApiClient
            {
                DispatchException = CreateConflictException(
                    payload: conflictView,
                    serviceName: "simulationsystems")
            };
            CitiesController controller =
                CreateCitiesController(environmentalConditionsClient: environmentalConditionsClient);

            ActionResult<CityUtilityIncidentStatusView> actionResult = await controller.DispatchDistrictUtilityResponse(
                cityId: cityId,
                request: new DispatchCityUtilityIncidentResponseRequest(
                    Focus: "CriticalInfrastructure",
                    Intensity: "Critical"),
                cancellationToken: CancellationToken.None);

            ConflictObjectResult conflict = Assert.IsType<ConflictObjectResult>(actionResult.Result);
            CityUtilityIncidentStatusView view = Assert.IsType<CityUtilityIncidentStatusView>(conflict.Value);
            Assert.Equal(
                expected: cityId,
                actual: view.CityId);
            Assert.Equal(
                expected: "Critical",
                actual: view.AppliedIntensity);
            Assert.Equal(
                expected: "Critical",
                actual: view.BudgetAuthorizedIntensity);
        }

        [Fact]
        public async Task DispatchDistrictResupply_WhenDownstreamReturnsConflict_MapsConflictBody()
        {
            var cityId = Guid.Parse("2023c998-6be5-44ef-8eb6-bfa942fd5d78");
            DispatchCityResupplyView conflictView = CreateDispatchCityResupplyView(
                cityId: cityId,
                requestedIntensity: "High",
                appliedIntensity: "High");
            var stockpilesClient = new RecordingStockpilesApiClient
            {
                DispatchException = CreateConflictException(
                    payload: conflictView,
                    serviceName: "resources")
            };
            CitiesController controller = CreateCitiesController(stockpilesClient: stockpilesClient);

            ActionResult<DispatchCityResupplyView> actionResult = await controller.DispatchDistrictResupply(
                cityId: cityId,
                request: new DispatchCityResupplyRequest(
                    Focus: ResupplyFocus.EmergencyWater,
                    Intensity: ResupplyIntensity.High),
                cancellationToken: CancellationToken.None);

            ConflictObjectResult conflict = Assert.IsType<ConflictObjectResult>(actionResult.Result);
            DispatchCityResupplyView view = Assert.IsType<DispatchCityResupplyView>(conflict.Value);
            Assert.Equal(
                expected: cityId,
                actual: view.CityId);
            Assert.Equal(
                expected: "High",
                actual: view.RequestedIntensity);
            Assert.Equal(
                expected: "High",
                actual: view.AppliedIntensity);
        }

        [Fact]
        public async Task GetResidentsPage_WhenCalled_UsesSimulationClockDate()
        {
            var cityId = Guid.Parse("fdac2b90-c8c4-4736-acd0-b5e9d02be1bf");
            DateTimeOffset simTimeUtc = new(
                year: 2048,
                month: 6,
                day: 7,
                hour: 23,
                minute: 45,
                second: 0,
                offset: TimeSpan.Zero);
            PagedResult<PersonDto> page = CreateResidentsPageResult();
            var simulationClient = new RecordingSimulationApiClient
            {
                ClockResult = CreateSimulationClockView(
                    simulationId: cityId,
                    simTimeUtc: simTimeUtc)
            };
            var populationClient = new RecordingPopulationApiClient
            {
                ResidentsPageResult = page
            };
            CitiesController controller = CreateCitiesController(
                simulationClient: simulationClient,
                populationClient: populationClient);

            ActionResult<PagedResult<PersonDto>> actionResult = await controller.GetResidentsPage(
                cityId: cityId,
                pageNumber: 3,
                pageSize: 40,
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            PagedResult<PersonDto> view = Assert.IsType<PagedResult<PersonDto>>(ok.Value);
            Assert.Same(
                expected: page,
                actual: view);
            Assert.Equal(
                expected: DateOnly.FromDateTime(simTimeUtc.UtcDateTime),
                actual: populationClient.LastResidentsPageCurrentDate);
            Assert.Equal(
                expected: 3,
                actual: populationClient.LastResidentsPageNumber);
            Assert.Equal(
                expected: 40,
                actual: populationClient.LastResidentsPageSize);
            Assert.Equal(
                expected: cityId,
                actual: populationClient.LastResidentsPageCityId);
        }

        [Fact]
        public async Task GetResidentDetails_WhenCalled_UsesSimulationClockDate()
        {
            var cityId = Guid.Parse("287886e2-296f-4c66-82b7-f9ff0d2c7d94");
            var personId = Guid.Parse("2ebfc973-bffe-4a22-9862-a62f6ee90ac1");
            DateTimeOffset simTimeUtc = new(
                year: 2048,
                month: 6,
                day: 8,
                hour: 6,
                minute: 10,
                second: 0,
                offset: TimeSpan.Zero);
            CityResidentDetailsDto resident = CreateCityResidentDetailsDto(personId);
            var simulationClient = new RecordingSimulationApiClient
            {
                ClockResult = CreateSimulationClockView(
                    simulationId: cityId,
                    simTimeUtc: simTimeUtc)
            };
            var populationClient = new RecordingPopulationApiClient
            {
                ResidentDetailsResult = resident
            };
            CitiesController controller = CreateCitiesController(
                simulationClient: simulationClient,
                populationClient: populationClient);

            ActionResult<CityResidentDetailsDto> actionResult = await controller.GetResidentDetails(
                cityId: cityId,
                personId: personId,
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            CityResidentDetailsDto view = Assert.IsType<CityResidentDetailsDto>(ok.Value);
            Assert.Same(
                expected: resident,
                actual: view);
            Assert.Equal(
                expected: DateOnly.FromDateTime(simTimeUtc.UtcDateTime),
                actual: populationClient.LastResidentDetailsCurrentDate);
            Assert.Equal(
                expected: cityId,
                actual: populationClient.LastResidentDetailsCityId);
            Assert.Equal(
                expected: personId,
                actual: populationClient.LastResidentDetailsPersonId);
        }

        [Fact]
        public async Task GetResidentEducationStatus_WhenAvailable_MapsEducationOwnerState()
        {
            Guid cityId = Guid.NewGuid();
            Guid residentId = Guid.NewGuid();
            Guid enrollmentId = Guid.NewGuid();
            Guid institutionId = Guid.NewGuid();
            var educationClient = new RecordingEducationApiClient
            {
                StudentStatusResult = new StudentEducationStatusResponse(
                    ResidentId: residentId,
                    IsAlive: true,
                    IsActive: true,
                    CompletedStage: "primary",
                    CompletedStageOn: new DateOnly(2047, 6, 30),
                    ActiveEnrollment: new ActiveStudentEnrollmentResponse(
                        EnrollmentId: enrollmentId,
                        InstitutionId: institutionId,
                        InstitutionName: "Central School",
                        InstitutionKind: "school",
                        LocationAnchorId: institutionId,
                        Stage: "secondary",
                        EnrolledOn: new DateOnly(2048, 5, 1)))
            };
            CitiesController controller = CreateCitiesController(educationClient: educationClient);

            ActionResult<CityResidentEducationStatusResponseDto> action =
                await controller.GetResidentEducationStatus(
                    cityId,
                    residentId,
                    CancellationToken.None);

            var response = Assert.IsType<CityResidentEducationStatusResponseDto>(
                Assert.IsType<OkObjectResult>(action.Result).Value);
            Assert.True(response.ProfileAvailable);
            Assert.Equal("primary", response.CompletedStage);
            Assert.NotNull(response.ActiveEnrollment);
            Assert.Equal(enrollmentId, response.ActiveEnrollment.EnrollmentId);
            Assert.Equal(institutionId, response.ActiveEnrollment.InstitutionId);
            Assert.Equal(cityId, educationClient.LastSimulationHostId);
            Assert.Equal(residentId, educationClient.LastStudentStatusResidentId);
        }

        [Fact]
        public async Task GetResidentEducationStatus_WhenProfileIsPending_ReturnsUnavailableState()
        {
            Guid cityId = Guid.NewGuid();
            Guid residentId = Guid.NewGuid();
            CitiesController controller = CreateCitiesController(
                educationClient: new RecordingEducationApiClient());

            ActionResult<CityResidentEducationStatusResponseDto> action =
                await controller.GetResidentEducationStatus(
                    cityId,
                    residentId,
                    CancellationToken.None);

            var response = Assert.IsType<CityResidentEducationStatusResponseDto>(
                Assert.IsType<OkObjectResult>(action.Result).Value);
            Assert.Equal(residentId, response.ResidentId);
            Assert.False(response.ProfileAvailable);
            Assert.Null(response.ActiveEnrollment);
        }

        [Fact]
        public async Task GetEducationCatalog_WhenCalled_UsesEducationOwnerCatalog()
        {
            var cityId = Guid.Parse("4314a9ae-e053-4f7b-9c52-0325c236f547");
            var institutionId = Guid.Parse("c5687145-3207-41dd-8827-d6143a815414");
            var educationClient = new RecordingEducationApiClient
            {
                CatalogResult = new EducationInstitutionCatalogResponse(
                    Institutions:
                    [
                        new EducationInstitutionResponse(
                            InstitutionId: institutionId,
                            Name: "Central Education Complex",
                            Kind: "school",
                            LocationAnchorId: institutionId,
                            Capacity: 640,
                            CurrentEnrollmentCount: 17,
                            AvailableSeatCount: 623)
                    ])
            };
            CitiesController controller = CreateCitiesController(educationClient: educationClient);

            ActionResult<CityEducationCatalogResponseDto> action = await controller.GetEducationCatalog(
                cityId: cityId,
                cancellationToken: CancellationToken.None);

            var response = Assert.IsType<CityEducationCatalogResponseDto>(
                Assert.IsType<OkObjectResult>(action.Result).Value);
            Assert.Equal(
                expected: cityId,
                actual: educationClient.LastSimulationHostId);
            CityEducationInstitutionResponseDto institution = Assert.Single(response.Institutions);
            Assert.Equal(
                expected: institutionId,
                actual: institution.InstitutionId);
            Assert.Equal(
                expected: "Central Education Complex",
                actual: institution.Name);
            Assert.Equal(
                expected: 623,
                actual: institution.AvailableSeatCount);
        }

        [Fact]
        public async Task EducationOperations_WhenCalled_UseOwnerAndSimulationDate()
        {
            var cityId = Guid.Parse("2c92d38c-32ac-4e9d-9e82-d137f00d5a8c");
            var residentId = Guid.Parse("a219b52c-69b9-4979-8536-92172a89d157");
            var institutionId = Guid.Parse("c4919001-3602-42d6-9c85-faf646a78f88");
            var enrollmentId = Guid.Parse("9f9ba8c8-0fe5-48b4-a79c-62e568a56b2c");
            DateTimeOffset simTimeUtc = new(
                year: 2048,
                month: 6,
                day: 8,
                hour: 6,
                minute: 10,
                second: 0,
                offset: TimeSpan.Zero);
            var simulationClient = new RecordingSimulationApiClient
            {
                ClockResult = CreateSimulationClockView(
                    simulationId: cityId,
                    simTimeUtc: simTimeUtc)
            };
            var educationClient = new RecordingEducationApiClient
            {
                EnrollResult = new EducationEnrollmentOperationResponse("Applied", enrollmentId),
                CompleteResult = new EducationEnrollmentOperationResponse(
                    "Applied",
                    enrollmentId,
                    "upper-secondary"),
                WithdrawResult = new EducationEnrollmentOperationResponse("NoActiveEnrollment")
            };
            CitiesController controller = CreateCitiesController(
                simulationClient: simulationClient,
                educationClient: educationClient);

            ActionResult<CityEducationOperationResponseDto> enrolled = await controller.EnrollResident(
                cityId: cityId,
                request: new EnrollCityResidentEducationRequestDto(
                    ResidentId: residentId,
                    InstitutionId: institutionId,
                    Stage: "upper-secondary"),
                cancellationToken: CancellationToken.None);
            ActionResult<CityEducationOperationResponseDto> completed = await controller.GraduateResident(
                cityId: cityId,
                request: new CompleteCityResidentEducationRequestDto(residentId),
                cancellationToken: CancellationToken.None);
            ActionResult<CityEducationOperationResponseDto> withdrawn = await controller.WithdrawResident(
                cityId: cityId,
                request: new WithdrawCityResidentEducationRequestDto(residentId),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: enrollmentId,
                actual: ReadEducationOperation(enrolled).EnrollmentId);
            Assert.Equal(
                expected: "upper-secondary",
                actual: ReadEducationOperation(completed).CompletedStage);
            Assert.Equal(
                expected: "NoActiveEnrollment",
                actual: ReadEducationOperation(withdrawn).Status);
            Assert.Equal(
                expected: 3,
                actual: simulationClient.GetClockCallCount);
            Assert.Equal(
                expected: cityId,
                actual: educationClient.LastSimulationHostId);
            Assert.Equal(
                expected: institutionId,
                actual: educationClient.LastEnrollRequest?.InstitutionId);
            Assert.Equal(
                expected: "upper-secondary",
                actual: educationClient.LastEnrollRequest?.Stage);
            DateOnly currentDate = DateOnly.FromDateTime(simTimeUtc.UtcDateTime);
            Assert.Equal(
                expected: currentDate,
                actual: educationClient.LastEnrollRequest?.EnrolledOn);
            Assert.Equal(
                expected: currentDate,
                actual: educationClient.LastCompleteRequest?.CompletedOn);
            Assert.Equal(
                expected: currentDate,
                actual: educationClient.LastWithdrawRequest?.WithdrawnOn);
        }

        private static CityEducationOperationResponseDto ReadEducationOperation(
            ActionResult<CityEducationOperationResponseDto> action)
        {
            return Assert.IsType<CityEducationOperationResponseDto>(
                Assert.IsType<OkObjectResult>(action.Result).Value);
        }
    }
}
