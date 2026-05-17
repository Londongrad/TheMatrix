using Matrix.ApiGateway.Controllers.Economy;
using Matrix.ApiGateway.Controllers.Population;
using Matrix.ApiGateway.Contracts.Economy;
using Matrix.ApiGateway.DownstreamClients.Economy;
using Matrix.ApiGateway.DownstreamClients.Population.People;
using Matrix.ApiGateway.DownstreamClients.Population.Person;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Economy.Contracts.Budget.Requests;
using Matrix.Economy.Contracts.Budget.Views;
using Matrix.Population.Contracts.Models;
using Matrix.Population.Contracts.Scenarios.ClassicCity.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Controllers;

public sealed class EconomyAndPopulationControllersTests
{
    [Fact]
    public async Task EconomyControllerGetSummary_WhenDownstreamReturnsNull_MapsBadGateway()
    {
        var controller = new EconomyController(new RecordingGatewayEconomyClient
        {
            SummaryResult = null
        });

        IActionResult actionResult = await controller.GetSummary(CancellationToken.None);

        StatusCodeResult status = Assert.IsType<StatusCodeResult>(actionResult);
        Assert.Equal(502, status.StatusCode);
    }

    [Fact]
    public async Task EconomyControllerGetBudgetLedgerFeed_ReturnsOk()
    {
        Guid cityId = Guid.Parse("6ebc93d3-f0cf-4dbe-bf79-3bcb958c3c46");
        var economyClient = new RecordingGatewayEconomyClient
        {
            BudgetLedgerResult = new CursorPagedResult<BudgetLedgerEntryView>([], 20, "next-cursor")
        };
        var controller = new EconomyController(economyClient);

        ActionResult<CursorPagedResult<BudgetLedgerEntryView>> actionResult = await controller.GetBudgetLedgerFeed(
            cityId,
            "cursor-1",
            20,
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        CursorPagedResult<BudgetLedgerEntryView> result = Assert.IsType<CursorPagedResult<BudgetLedgerEntryView>>(ok.Value);
        Assert.Equal("next-cursor", result.NextCursor);
        Assert.Equal(cityId, economyClient.LastBudgetLedgerCityId);
        Assert.Equal("cursor-1", economyClient.LastBudgetLedgerCursor);
    }

    [Fact]
    public async Task EconomyControllerHealth_WhenClientIsUnhealthy_ReturnsDegradedStatus()
    {
        var controller = new EconomyController(new RecordingGatewayEconomyClient
        {
            HealthResult = false
        });

        IActionResult actionResult = await controller.Health(CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("degraded", GetAnonymousProperty<string>(ok.Value, "status"));
    }

    [Fact]
    public async Task PopulationControllerInitializePopulation_ReturnsOk()
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
        var controller = new PopulationController(populationClient);
        InitializeCityPopulationRequest request = new(
            CityId: bootstrap.CityId,
            CurrentDate: new DateOnly(2048, 6, 1),
            CreatedAtUtc: new DateTimeOffset(2048, 6, 1, 8, 0, 0, TimeSpan.Zero),
            PeopleCount: 10000,
            RandomSeed: 17,
            Environment: new CityPopulationEnvironmentDto("Temperate", "Northern", 540),
            Tuning: new CityPopulationBootstrapTuningDto(50, 50, 50, 50),
            CityAnchors: null,
            ResidentialBuildings: null);

        ActionResult<CityPopulationBootstrapSummaryDto> actionResult = await controller.InitializePopulation(request, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        CityPopulationBootstrapSummaryDto result = Assert.IsType<CityPopulationBootstrapSummaryDto>(ok.Value);
        Assert.Equal(bootstrap.CityId, result.CityId);
        Assert.Same(request, populationClient.LastBootstrapRequest);
    }

    [Fact]
    public async Task PopulationControllerGetCitizensPage_ReturnsOk()
    {
        var populationClient = new RecordingGatewayPopulationClient
        {
            CitizensPageResult = CreateResidentsPageResult()
        };
        var controller = new PopulationController(populationClient);

        ActionResult<PagedResult<PersonDto>> actionResult = await controller.GetCitizensPage(3, 40, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        PagedResult<PersonDto> page = Assert.IsType<PagedResult<PersonDto>>(ok.Value);
        Assert.Equal(2, page.PageNumber);
        Assert.Equal(3, populationClient.LastCitizensPageNumber);
        Assert.Equal(40, populationClient.LastCitizensPageSize);
    }

    [Fact]
    public async Task PersonControllerKillPerson_ReturnsOk()
    {
        Guid personId = Guid.Parse("d20b7cbe-f341-46a8-8b59-b86b3ad65261");
        PersonDto person = Assert.Single(CreateResidentsPageResult(personId).Items);
        var personClient = new RecordingGatewayPersonClient
        {
            KillResult = person
        };
        var controller = new PersonController(personClient);

        IActionResult actionResult = await controller.KillPerson(personId, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult);
        PersonDto result = Assert.IsType<PersonDto>(ok.Value);
        Assert.Equal(personId, result.Id);
        Assert.Equal(personId, personClient.LastKilledPersonId);
    }

    private static T GetAnonymousProperty<T>(object? source, string propertyName)
    {
        object? value = source?.GetType().GetProperty(propertyName)?.GetValue(source);
        return Assert.IsType<T>(value);
    }

    private sealed class RecordingGatewayEconomyClient : IEconomyApiClient
    {
        public EconomySummaryView? SummaryResult { get; set; } = CreateEconomySummaryView();
        public bool HealthResult { get; set; } = true;
        public CursorPagedResult<BudgetLedgerEntryView>? BudgetLedgerResult { get; set; }
        public Guid? LastBudgetLedgerCityId { get; private set; }
        public string? LastBudgetLedgerCursor { get; private set; }

        public Task<EconomySummaryView?> GetSummaryAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(SummaryResult);

        public Task<EconomySummaryView?> GetCitySummaryAsync(Guid cityId, CancellationToken cancellationToken = default)
            => Task.FromResult(SummaryResult);

        public Task<CityOperationalBudgetPressureView?> GetCityOperationalBudgetPressureAsync(Guid cityId, CancellationToken cancellationToken = default)
            => Task.FromResult<CityOperationalBudgetPressureView?>(null);

        public Task<IReadOnlyList<CityBusinessView>> GetCityBusinessesAsync(Guid cityId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CityBusinessView>>([]);

        public Task<IReadOnlyList<CityHouseholdAccountView>> GetCityHouseholdAccountsAsync(Guid cityId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CityHouseholdAccountView>>([]);

        public Task<CursorPagedResult<BudgetLedgerEntryView>> GetCityBudgetLedgerFeedAsync(Guid cityId, string? cursor = null, int pageSize = 50, CancellationToken cancellationToken = default)
        {
            LastBudgetLedgerCityId = cityId;
            LastBudgetLedgerCursor = cursor;
            return Task.FromResult(BudgetLedgerResult ?? new CursorPagedResult<BudgetLedgerEntryView>([], pageSize, null));
        }

        public Task<CursorPagedResult<CityBusinessLedgerEntryView>> GetCityBusinessLedgerFeedAsync(Guid businessId, string? cursor = null, int pageSize = 50, CancellationToken cancellationToken = default)
            => Task.FromResult(new CursorPagedResult<CityBusinessLedgerEntryView>([], pageSize, null));

        public Task<CursorPagedResult<CityHouseholdAccountLedgerEntryView>> GetCityHouseholdAccountLedgerFeedAsync(Guid householdAccountId, string? cursor = null, int pageSize = 50, CancellationToken cancellationToken = default)
            => Task.FromResult(new CursorPagedResult<CityHouseholdAccountLedgerEntryView>([], pageSize, null));

        public Task<CityEconomyBootstrapResultView> InitializeCityEconomyAsync(Guid cityId, InitializeCityEconomyRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> HealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(HealthResult);
    }

    private sealed class RecordingGatewayPopulationClient : IPopulationApiClient
    {
        public CityPopulationBootstrapSummaryDto? BootstrapResult { get; set; }
        public PagedResult<PersonDto>? CitizensPageResult { get; set; }
        public InitializeCityPopulationRequest? LastBootstrapRequest { get; private set; }
        public int? LastCitizensPageNumber { get; private set; }
        public int? LastCitizensPageSize { get; private set; }

        public Task<CityPopulationBootstrapSummaryDto> InitializeCityPopulationAsync(InitializeCityPopulationRequest request, CancellationToken cancellationToken = default)
        {
            LastBootstrapRequest = request;
            return Task.FromResult(BootstrapResult ?? throw new InvalidOperationException("BootstrapResult was not configured."));
        }

        public Task<CityPopulationSummaryDto> GetCityPopulationSummaryAsync(Guid cityId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CityPopulationDashboardDto> GetCityPopulationDashboardAsync(Guid cityId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CityPopulationDistrictPressureDto> GetCityDistrictPressureAsync(Guid cityId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PagedResult<PersonDto>> GetCityResidentsPageAsync(Guid cityId, DateOnly currentDate, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CityResidentDetailsDto> GetCityResidentDetailsAsync(Guid cityId, Guid personId, DateOnly currentDate, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CityEmploymentCatalogDto> GetCityEmploymentCatalogAsync(Guid cityId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CityEducationCatalogDto> GetCityEducationCatalogAsync(Guid cityId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CityEmploymentOperationResultDto> HireCityResidentAsync(Guid cityId, CityEmploymentOperationRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CityEmploymentOperationResultDto> FireCityResidentAsync(Guid cityId, CityEmploymentOperationRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CityEmploymentOperationResultDto> RetireCityResidentAsync(Guid cityId, CityEmploymentOperationRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CityEducationOperationResultDto> EnrollCityResidentAsync(Guid cityId, CityEducationOperationRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CityEducationOperationResultDto> GraduateCityResidentAsync(Guid cityId, CityEducationOperationRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CityEducationOperationResultDto> WithdrawCityResidentFromStudyAsync(Guid cityId, CityEducationOperationRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CityCivilRegistryOperationResultDto> RegisterCityMarriageAsync(Guid cityId, CityCivilRegistryOperationRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<CityCivilRegistryOperationResultDto> RegisterCityDivorceAsync(Guid cityId, CityCivilRegistryOperationRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PagedResult<PersonDto>> GetCitizensPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            LastCitizensPageNumber = pageNumber;
            LastCitizensPageSize = pageSize;
            return Task.FromResult(CitizensPageResult ?? new PagedResult<PersonDto>([], 0, pageNumber, pageSize));
        }
    }

    private sealed class RecordingGatewayPersonClient : IPersonApiClient
    {
        public PersonDto? KillResult { get; set; }
        public Guid? LastKilledPersonId { get; private set; }

        public Task<PersonDto> KillAsync(Guid personId, CancellationToken cancellationToken = default)
        {
            LastKilledPersonId = personId;
            return Task.FromResult(KillResult ?? throw new InvalidOperationException("KillResult was not configured."));
        }

        public Task<PersonDto> ResurrectAsync(Guid personId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
