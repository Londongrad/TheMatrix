using Matrix.ApiGateway.Contracts.SimulationCore.Scenarios.ClassicCity.Economy;
using Matrix.ApiGateway.Controllers.Economy.Scenarios.ClassicCity;
using Matrix.ApiGateway.Controllers.Population;
using Matrix.ApiGateway.Controllers.SimulationCore.Scenarios.ClassicCity.Economy;
using Matrix.ApiGateway.Controllers.SimulationCore.Scenarios.ClassicCity.Population;
using Matrix.ApiGateway.DownstreamClients.Economy.Scenarios.ClassicCity;
using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.ApiGateway.DownstreamClients.Population.Scenarios.ClassicCity;
using Matrix.ApiGateway.DownstreamClients.Population.Person;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Contracts.Scenarios.ClassicCity.Budget.Requests;
using Matrix.Economy.Contracts.Scenarios.ClassicCity.Budget.Views;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Controllers
{
    public sealed class EconomyAndPopulationControllersTests
    {
        [Fact]
        public void ClassicCityEconomyController_UsesScenarioRoute()
        {
            RouteAttribute route = Assert.Single(
                typeof(ClassicCityEconomyController)
                   .GetCustomAttributes(
                        attributeType: typeof(RouteAttribute),
                        inherit: true)
                   .Cast<RouteAttribute>());

            Assert.Equal(
                expected: "api/scenarios/classic-city/economy",
                actual: route.Template);
        }

        [Fact]
        public void ClassicCityPopulationController_UsesScenarioRoute()
        {
            RouteAttribute route = Assert.Single(
                typeof(ClassicCityPopulationController)
                   .GetCustomAttributes(
                        attributeType: typeof(RouteAttribute),
                        inherit: true)
                   .Cast<RouteAttribute>());

            Assert.Equal(
                expected: "api/scenarios/classic-city/population",
                actual: route.Template);
        }

        [Fact]
        public async Task EconomyControllerGetSummary_WhenDownstreamReturnsNull_MapsBadGateway()
        {
            var controller = new EconomyController(
                new RecordingGatewayEconomyClient
                {
                    SummaryResult = null
                });

            IActionResult actionResult = await controller.GetSummary(CancellationToken.None);

            StatusCodeResult status = Assert.IsType<StatusCodeResult>(actionResult);
            Assert.Equal(
                expected: 502,
                actual: status.StatusCode);
        }

        [Fact]
        public async Task EconomyControllerGetBudgetLedgerFeed_ReturnsOk()
        {
            var cityId = Guid.Parse("6ebc93d3-f0cf-4dbe-bf79-3bcb958c3c46");
            var economyClient = new RecordingGatewayEconomyClient
            {
                BudgetLedgerResult = new CursorPagedResult<BudgetLedgerEntryView>(
                    items: [],
                    pageSize: 20,
                    nextCursor: "next-cursor")
            };
            var controller = new ClassicCityEconomyController(economyClient);

            ActionResult<CursorPagedResult<BudgetLedgerEntryView>> actionResult = await controller.GetBudgetLedgerFeed(
                cityId: cityId,
                cursor: "cursor-1",
                pageSize: 20,
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            CursorPagedResult<BudgetLedgerEntryView> result =
                Assert.IsType<CursorPagedResult<BudgetLedgerEntryView>>(ok.Value);
            Assert.Equal(
                expected: "next-cursor",
                actual: result.NextCursor);
            Assert.Equal(
                expected: cityId,
                actual: economyClient.LastBudgetLedgerCityId);
            Assert.Equal(
                expected: "cursor-1",
                actual: economyClient.LastBudgetLedgerCursor);
        }

        [Fact]
        public async Task EconomyControllerHealth_WhenClientIsUnhealthy_ReturnsDegradedStatus()
        {
            var controller = new EconomyController(
                new RecordingGatewayEconomyClient
                {
                    HealthResult = false
                });

            IActionResult actionResult = await controller.Health(CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Equal(
                expected: "degraded",
                actual: GetAnonymousProperty<string>(
                    source: ok.Value,
                    propertyName: "status"));
        }

        [Fact]
        public async Task ClassicCityPopulationControllerInitializePopulation_ReturnsOk()
        {
            CityPopulationBootstrapSummaryDto bootstrap = new(
                CityId: Guid.Parse("49e37e71-2572-47b3-a253-b3e348fd79fd"),
                RequestedPeopleCount: 10000,
                GeneratedPeopleCount: 9800,
                HouseholdCount: 3500,
                HousedHouseholdCount: 3400,
                HomelessHouseholdCount: 100,
                HousedPeopleCount: 9700,
                HomelessPeopleCount: 100);
            var populationClient = new RecordingGatewayPopulationClient
            {
                BootstrapResult = bootstrap
            };
            var controller = new ClassicCityPopulationController(populationClient);
            InitializeCityPopulationRequest request = new(
                CityId: bootstrap.CityId,
                CurrentDate: new DateOnly(
                    year: 2048,
                    month: 6,
                    day: 1),
                CreatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 6,
                    day: 1,
                    hour: 8,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                PeopleCount: 10000,
                RandomSeed: 17,
                Environment: new CityPopulationEnvironmentDto(
                    ClimateZone: "Temperate",
                    Hemisphere: "Northern",
                    UtcOffsetMinutes: 540),
                Tuning: new CityPopulationBootstrapTuningDto(
                    HousingPressurePercent: 50,
                    EconomicStabilityPercent: 50,
                    SocialVolatilityPercent: 50,
                    FamilyFormationPercent: 50),
                CityAnchors: null,
                ResidentialBuildings: null);

            ActionResult<CityPopulationBootstrapSummaryDto> actionResult = await controller.InitializePopulation(
                request: request,
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            CityPopulationBootstrapSummaryDto result = Assert.IsType<CityPopulationBootstrapSummaryDto>(ok.Value);
            Assert.Equal(
                expected: bootstrap.CityId,
                actual: result.CityId);
            Assert.Same(
                expected: request,
                actual: populationClient.LastBootstrapRequest);
        }

        [Fact]
        public async Task PopulationControllerGetPeoplePage_ReturnsOk()
        {
            var populationClient = new RecordingGatewayPopulationClient
            {
                PeoplePageResult = CreateResidentsPageResult()
            };
            var controller = new PopulationController(populationClient);

            ActionResult<PagedResult<PersonDto>> actionResult = await controller.GetPeoplePage(
                pageNumber: 3,
                pageSize: 40,
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            PagedResult<PersonDto> page = Assert.IsType<PagedResult<PersonDto>>(ok.Value);
            Assert.Equal(
                expected: 2,
                actual: page.PageNumber);
            Assert.Equal(
                expected: 3,
                actual: populationClient.LastPeoplePageNumber);
            Assert.Equal(
                expected: 40,
                actual: populationClient.LastPeoplePageSize);
        }

        [Fact]
        public async Task PersonControllerKillPerson_ReturnsOk()
        {
            var personId = Guid.Parse("d20b7cbe-f341-46a8-8b59-b86b3ad65261");
            PersonDto person = Assert.Single(
                CreateResidentsPageResult(personId)
                   .Items);
            var personClient = new RecordingGatewayPersonClient
            {
                KillResult = person
            };
            var controller = new PersonController(personClient);

            IActionResult actionResult = await controller.KillPerson(
                personId: personId,
                cancellationToken: CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult);
            PersonDto result = Assert.IsType<PersonDto>(ok.Value);
            Assert.Equal(
                expected: personId,
                actual: result.Id);
            Assert.Equal(
                expected: personId,
                actual: personClient.LastKilledPersonId);
        }

        private static T GetAnonymousProperty<T>(
            object? source,
            string propertyName)
        {
            object? value = source?.GetType()
               .GetProperty(propertyName)
              ?.GetValue(source);
            return Assert.IsType<T>(value);
        }

        private sealed class RecordingGatewayEconomyClient : IClassicCityEconomyApiClient
        {
            public EconomySummaryView? SummaryResult { get; set; } = CreateEconomySummaryView();
            public bool HealthResult { get; set; } = true;
            public CursorPagedResult<BudgetLedgerEntryView>? BudgetLedgerResult { get; set; }
            public Guid? LastBudgetLedgerCityId { get; private set; }
            public string? LastBudgetLedgerCursor { get; private set; }

            public Task<EconomySummaryView?> GetSummaryAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(SummaryResult);
            }

            public Task<EconomySummaryView?> GetCitySummaryAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(SummaryResult);
            }

            public Task<CityOperationalBudgetPressureView?> GetCityOperationalBudgetPressureAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<CityOperationalBudgetPressureView?>(null);
            }

            public Task<IReadOnlyList<CityBusinessView>> GetCityBusinessesAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<CityBusinessView>>([]);
            }

            public Task<IReadOnlyList<CityHouseholdAccountView>> GetCityHouseholdAccountsAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<CityHouseholdAccountView>>([]);
            }

            public Task<CursorPagedResult<BudgetLedgerEntryView>> GetCityBudgetLedgerFeedAsync(
                Guid cityId,
                string? cursor = null,
                int pageSize = 50,
                CancellationToken cancellationToken = default)
            {
                LastBudgetLedgerCityId = cityId;
                LastBudgetLedgerCursor = cursor;
                return Task.FromResult(
                    BudgetLedgerResult ??
                    new CursorPagedResult<BudgetLedgerEntryView>(
                        items: [],
                        pageSize: pageSize,
                        nextCursor: null));
            }

            public Task<CursorPagedResult<CityBusinessLedgerEntryView>> GetCityBusinessLedgerFeedAsync(
                Guid businessId,
                string? cursor = null,
                int pageSize = 50,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(
                    new CursorPagedResult<CityBusinessLedgerEntryView>(
                        items: [],
                        pageSize: pageSize,
                        nextCursor: null));
            }

            public Task<CursorPagedResult<CityHouseholdAccountLedgerEntryView>> GetCityHouseholdAccountLedgerFeedAsync(
                Guid householdAccountId,
                string? cursor = null,
                int pageSize = 50,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(
                    new CursorPagedResult<CityHouseholdAccountLedgerEntryView>(
                        items: [],
                        pageSize: pageSize,
                        nextCursor: null));
            }

            public Task<CityEconomyBootstrapResultView> InitializeCityEconomyAsync(
                Guid cityId,
                InitializeCityEconomyRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<bool> HealthAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(HealthResult);
            }
        }

        private sealed class RecordingGatewayPopulationClient : IPopulationApiClient, IClassicCityPopulationApiClient
        {
            public CityPopulationBootstrapSummaryDto? BootstrapResult { get; set; }
            public PagedResult<PersonDto>? PeoplePageResult { get; set; }
            public InitializeCityPopulationRequest? LastBootstrapRequest { get; private set; }
            public int? LastPeoplePageNumber { get; private set; }
            public int? LastPeoplePageSize { get; private set; }

            public Task<CityPopulationBootstrapSummaryDto> InitializeCityPopulationAsync(
                InitializeCityPopulationRequest request,
                CancellationToken cancellationToken = default)
            {
                LastBootstrapRequest = request;
                return Task.FromResult(
                    BootstrapResult ?? throw new InvalidOperationException("BootstrapResult was not configured."));
            }

            public Task<CityPopulationSummaryDto> GetCityPopulationSummaryAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityPopulationDashboardDto> GetCityPopulationDashboardAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityPopulationDistrictPressureDto> GetCityDistrictPressureAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<PagedResult<PersonDto>> GetCityResidentsPageAsync(
                Guid cityId,
                DateOnly currentDate,
                int pageNumber,
                int pageSize,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityResidentDetailsDto> GetCityResidentDetailsAsync(
                Guid cityId,
                Guid personId,
                DateOnly currentDate,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityEmploymentCatalogDto> GetCityEmploymentCatalogAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityEducationCatalogDto> GetCityEducationCatalogAsync(
                Guid cityId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityEmploymentOperationResultDto> HireCityResidentAsync(
                Guid cityId,
                CityEmploymentOperationRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityEmploymentOperationResultDto> FireCityResidentAsync(
                Guid cityId,
                CityEmploymentOperationRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityEmploymentOperationResultDto> RetireCityResidentAsync(
                Guid cityId,
                CityEmploymentOperationRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityEducationOperationResultDto> EnrollCityResidentAsync(
                Guid cityId,
                CityEducationOperationRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityEducationOperationResultDto> GraduateCityResidentAsync(
                Guid cityId,
                CityEducationOperationRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityEducationOperationResultDto> WithdrawCityResidentFromStudyAsync(
                Guid cityId,
                CityEducationOperationRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityCivilRegistryOperationResultDto> RegisterCityMarriageAsync(
                Guid cityId,
                CityCivilRegistryOperationRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CityCivilRegistryOperationResultDto> RegisterCityDivorceAsync(
                Guid cityId,
                CityCivilRegistryOperationRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<PagedResult<PersonDto>> GetPeoplePageAsync(
                int pageNumber,
                int pageSize,
                CancellationToken cancellationToken = default)
            {
                LastPeoplePageNumber = pageNumber;
                LastPeoplePageSize = pageSize;
                return Task.FromResult(
                    PeoplePageResult ??
                    new PagedResult<PersonDto>(
                        items: [],
                        totalCount: 0,
                        pageNumber: pageNumber,
                        pageSize: pageSize));
            }
        }

        private sealed class RecordingGatewayPersonClient : IPersonApiClient
        {
            public PersonDto? KillResult { get; set; }
            public Guid? LastKilledPersonId { get; private set; }

            public Task<PersonDto> KillAsync(
                Guid personId,
                CancellationToken cancellationToken = default)
            {
                LastKilledPersonId = personId;
                return Task.FromResult(
                    KillResult ?? throw new InvalidOperationException("KillResult was not configured."));
            }

            public Task<PersonDto> ResurrectAsync(
                Guid personId,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }
    }
}
