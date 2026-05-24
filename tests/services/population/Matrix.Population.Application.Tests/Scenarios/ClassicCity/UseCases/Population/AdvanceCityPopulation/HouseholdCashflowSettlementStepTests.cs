using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Xunit;
using static Matrix.Population.Application.Tests.TestSupport.PopulationApplicationTestSupport;
using HouseholdEntity = Matrix.Population.Domain.Entities.Household;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;

public sealed class HouseholdCashflowSettlementStepTests
{
    private static readonly CityId TestCityId = CityId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
    private static readonly DateOnly PreviousDate = new(2048, 5, 1);
    private static readonly DateOnly CurrentDate = new(2048, 5, 4);
    private static readonly DateTimeOffset CreatedAtUtc = new(2048, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private const int SettledDays = 3;
    private const decimal RetailTaxRate = 0.08m;

    [Fact]
    public async Task ApplyAsync_WhenCurrentDateDoesNotAdvance_ReturnsNullAndDoesNotAddItems()
    {
        HouseholdId householdId = CreateHouseholdId(1);
        HouseholdEntity household = CreateHousehold(householdId);
        PersonEntity resident = CreateRetiredResident(householdId, personIndex: 1);
        Money beforeCashReserve = household.CashReserve;
        var cashflowItems = new List<ClassicCityHouseholdCashflowSettlementItemV1>();
        var workplacePayrollItems = new List<ClassicCityWorkplacePayrollSettlementItemV1>();
        var routingService = new RecordingCommuteRoutingService();

        CityEconomyDailySettlementSnapshot? result = await ApplyAsync(
            householdsById: new Dictionary<HouseholdId, HouseholdEntity>
            {
                [householdId] = household
            },
            residentsByHouseholdId: new Dictionary<HouseholdId, IReadOnlyCollection<PersonEntity>>
            {
                [householdId] = [resident]
            },
            previousDate: CurrentDate,
            currentDate: CurrentDate,
            commuteRoutingService: routingService,
            cashflowItems: cashflowItems,
            workplacePayrollItems: workplacePayrollItems);

        Assert.Null(result);
        Assert.Empty(cashflowItems);
        Assert.Empty(workplacePayrollItems);
        Assert.Empty(routingService.EmploymentCommuteResidents);
        Assert.Equal(beforeCashReserve, household.CashReserve);
    }

    [Fact]
    public async Task ApplyAsync_WhenHouseholdsHaveNoResidents_ReturnsNull()
    {
        HouseholdId householdId = CreateHouseholdId(1);
        var cashflowItems = new List<ClassicCityHouseholdCashflowSettlementItemV1>();
        var workplacePayrollItems = new List<ClassicCityWorkplacePayrollSettlementItemV1>();
        var routingService = new RecordingCommuteRoutingService();

        CityEconomyDailySettlementSnapshot? result = await ApplyAsync(
            householdsById: new Dictionary<HouseholdId, HouseholdEntity>
            {
                [householdId] = CreateHousehold(householdId)
            },
            residentsByHouseholdId: new Dictionary<HouseholdId, IReadOnlyCollection<PersonEntity>>(),
            commuteRoutingService: routingService,
            cashflowItems: cashflowItems,
            workplacePayrollItems: workplacePayrollItems);

        Assert.Null(result);
        Assert.Empty(cashflowItems);
        Assert.Empty(workplacePayrollItems);
        Assert.Empty(routingService.EmploymentCommuteResidents);
    }

    [Fact]
    public async Task ApplyAsync_WhenHouseholdHasSupportIncome_AddsHouseholdCashflowItemAndSnapshot()
    {
        HouseholdId householdId = CreateHouseholdId(1);
        HouseholdEntity household = CreateHousehold(householdId);
        PersonEntity resident = CreateRetiredResident(householdId, personIndex: 1);
        var cashflowItems = new List<ClassicCityHouseholdCashflowSettlementItemV1>();
        var workplacePayrollItems = new List<ClassicCityWorkplacePayrollSettlementItemV1>();
        var policy = new CityHouseholdCashflowPolicy();
        CityHouseholdCashflowProfile expectedCashflow = policy.Build(
            householdResidents: [resident],
            housingStatus: HousingStatus.Housed,
            currentDate: CurrentDate,
            costOfLivingState: null);
        CityResidentIncomeSettlementProfile expectedIncome = policy.BuildResidentIncome(
            resident: resident,
            currentDate: CurrentDate,
            costOfLivingState: null,
            incomeMultiplier: 1m);

        CityEconomyDailySettlementSnapshot? result = await ApplyAsync(
            householdsById: new Dictionary<HouseholdId, HouseholdEntity>
            {
                [householdId] = household
            },
            residentsByHouseholdId: new Dictionary<HouseholdId, IReadOnlyCollection<PersonEntity>>
            {
                [householdId] = [resident]
            },
            housingByHouseholdId: new Dictionary<HouseholdId, HousingStatus>
            {
                [householdId] = HousingStatus.Housed
            },
            cashflowItems: cashflowItems,
            workplacePayrollItems: workplacePayrollItems);

        ClassicCityHouseholdCashflowSettlementItemV1 item = Assert.Single(cashflowItems);
        Assert.Equal(householdId.Value, item.HouseholdId);
        Assert.Equal(ClassicCityEconomySettlementBatchFactory.BuildHouseholdExternalReferenceCode(householdId), item.ExternalReferenceCode);
        Assert.Equal(expectedIncome.GrossIncome.Multiply(SettledDays).Amount, item.GrossPayrollAmount);
        Assert.Equal(expectedIncome.TaxWithheld.Multiply(SettledDays).Amount, item.IncomeTaxAmount);
        Assert.Equal(expectedIncome.NetIncome.Multiply(SettledDays).Amount, item.NetPayrollAmount);
        Assert.Equal(expectedCashflow.RetailTurnover.Multiply(SettledDays).Amount, item.RetailTurnoverAmount);
        Assert.Equal(expectedCashflow.RetailTurnover.Multiply(SettledDays).Multiply(RetailTaxRate).Amount, item.RetailTaxAmount);
        Assert.Equal(expectedCashflow.RetailStoreSpend.Multiply(SettledDays).Amount, item.RetailStoreSpendAmount);
        Assert.Equal(expectedCashflow.ServiceSpend.Multiply(SettledDays).Amount, item.ServiceSpendAmount);
        Assert.Equal(expectedCashflow.MunicipalSpend.Multiply(SettledDays).Amount, item.MunicipalSpendAmount);
        Assert.Empty(workplacePayrollItems);

        Assert.NotNull(result);
        Assert.Equal(CurrentDate, result!.CurrentDate);
        Assert.Equal(SettledDays, result.SettledDays);
        Assert.Equal(1, result.HouseholdCount);
        Assert.Equal(expectedCashflow.ResidentCount, result.ResidentCount);
        Assert.Equal(expectedIncome.GrossIncome.Multiply(SettledDays), result.GrossPayroll);
        Assert.Equal(expectedIncome.TaxWithheld.Multiply(SettledDays), result.IncomeTax);
        Assert.Equal(expectedIncome.NetIncome.Multiply(SettledDays), result.NetPayroll);
        Assert.Equal(expectedCashflow.RetailTurnover.Multiply(SettledDays), result.RetailTurnover);
        Assert.Equal(expectedCashflow.RetailTurnover.Multiply(SettledDays).Multiply(RetailTaxRate), result.RetailTax);
        Assert.Equal(expectedCashflow.HousingExpense.Multiply(SettledDays), result.HousingSpend);
    }

    [Fact]
    public async Task ApplyAsync_WhenResidentIsEmployedWithJob_AddsWorkplacePayrollItemAndUsesEmploymentCommute()
    {
        HouseholdId householdId = CreateHouseholdId(1);
        ResidentialBuildingId residentialBuildingId = ResidentialBuildingId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        Job job = CreateJob("Engineer", index: 1);
        PersonEntity resident = CreateEmployedResident(
            householdId: householdId,
            personIndex: 1,
            job: job);
        var cashflowItems = new List<ClassicCityHouseholdCashflowSettlementItemV1>();
        var workplacePayrollItems = new List<ClassicCityWorkplacePayrollSettlementItemV1>();
        var routingService = new RecordingCommuteRoutingService
        {
            EmploymentContext = CityPopulationCommuteContext.Blocked
        };
        var cashflowPolicy = new CityHouseholdCashflowPolicy();
        var participationPolicy = new CityPopulationParticipationPolicy();
        CityPopulationParticipationProfile poorCommuteProfile = participationPolicy.ResolveEmploymentProfile(
            person: resident,
            currentDate: CurrentDate,
            housingStatus: HousingStatus.Housed,
            livingConditions: CreateDefaultLivingConditions(),
            essentials: CreateDefaultEssentials(),
            commute: CityPopulationCommuteContext.Blocked);
        CityPopulationParticipationProfile neutralCommuteProfile = participationPolicy.ResolveEmploymentProfile(
            person: resident,
            currentDate: CurrentDate,
            housingStatus: HousingStatus.Housed,
            livingConditions: CreateDefaultLivingConditions(),
            essentials: CreateDefaultEssentials(),
            commute: CityPopulationCommuteContext.Neutral);
        CityResidentIncomeSettlementProfile expectedIncome = cashflowPolicy.BuildResidentIncome(
            resident: resident,
            currentDate: CurrentDate,
            costOfLivingState: null,
            incomeMultiplier: poorCommuteProfile.PayrollMultiplier);
        CityResidentIncomeSettlementProfile neutralIncome = cashflowPolicy.BuildResidentIncome(
            resident: resident,
            currentDate: CurrentDate,
            costOfLivingState: null,
            incomeMultiplier: neutralCommuteProfile.PayrollMultiplier);

        CityEconomyDailySettlementSnapshot? result = await ApplyAsync(
            householdsById: new Dictionary<HouseholdId, HouseholdEntity>
            {
                [householdId] = CreateHousehold(householdId)
            },
            residentsByHouseholdId: new Dictionary<HouseholdId, IReadOnlyCollection<PersonEntity>>
            {
                [householdId] = [resident]
            },
            housingByHouseholdId: new Dictionary<HouseholdId, HousingStatus>
            {
                [householdId] = HousingStatus.Housed
            },
            residentialBuildingIdByHouseholdId: new Dictionary<HouseholdId, ResidentialBuildingId?>
            {
                [householdId] = residentialBuildingId
            },
            commuteRoutingService: routingService,
            cashflowItems: cashflowItems,
            workplacePayrollItems: workplacePayrollItems);

        Assert.Equal([resident], routingService.EmploymentCommuteResidents);
        ResidentialBuildingId? resolvedResidentialBuildingId = Assert.Single(routingService.EmploymentResidentialBuildingIds);
        Assert.Equal(residentialBuildingId, resolvedResidentialBuildingId);

        ClassicCityWorkplacePayrollSettlementItemV1 payroll = Assert.Single(workplacePayrollItems);
        Assert.Equal(householdId.Value, payroll.HouseholdId);
        Assert.Equal(ClassicCityEconomySettlementBatchFactory.BuildHouseholdExternalReferenceCode(householdId), payroll.HouseholdExternalReferenceCode);
        Assert.Equal(job.WorkplaceId.Value, payroll.WorkplaceId);
        Assert.Equal(ClassicCityWorkplaceBusinessSyncBatchFactory.BuildExternalReferenceCode(job.WorkplaceId), payroll.WorkplaceExternalReferenceCode);
        Assert.Equal(job.Title, payroll.JobTitle);
        Assert.Equal(expectedIncome.GrossIncome.Multiply(SettledDays).Amount, payroll.GrossPayrollAmount);
        Assert.Equal(expectedIncome.TaxWithheld.Multiply(SettledDays).Amount, payroll.IncomeTaxAmount);
        Assert.Equal(expectedIncome.NetIncome.Multiply(SettledDays).Amount, payroll.NetPayrollAmount);
        Assert.True(payroll.NetPayrollAmount <= neutralIncome.NetIncome.Multiply(SettledDays).Amount);

        ClassicCityHouseholdCashflowSettlementItemV1 cashflowItem = Assert.Single(cashflowItems);
        Assert.Equal(0m, cashflowItem.GrossPayrollAmount);
        Assert.Equal(0m, cashflowItem.IncomeTaxAmount);
        Assert.Equal(0m, cashflowItem.NetPayrollAmount);
        Assert.NotNull(result);
        Assert.Equal(Money.Zero, result!.GrossPayroll);
        Assert.Equal(Money.Zero, result.IncomeTax);
        Assert.Equal(Money.Zero, result.NetPayroll);
    }

    private static Task<CityEconomyDailySettlementSnapshot?> ApplyAsync(
        IReadOnlyDictionary<HouseholdId, HouseholdEntity> householdsById,
        IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<PersonEntity>> residentsByHouseholdId,
        IReadOnlyDictionary<HouseholdId, HousingStatus>? housingByHouseholdId = null,
        IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?>? residentialBuildingIdByHouseholdId = null,
        DateOnly? previousDate = null,
        DateOnly? currentDate = null,
        CityPopulationCostOfLivingState? costOfLivingState = null,
        CityPopulationEssentialsState? essentialsState = null,
        CityPopulationLivingConditionsState? livingConditionsState = null,
        IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>? districtUtilityConditionsByDistrictId = null,
        IReadOnlyDictionary<HouseholdId, DistrictId?>? districtByHouseholdId = null,
        RecordingCommuteRoutingService? commuteRoutingService = null,
        ICollection<ClassicCityHouseholdCashflowSettlementItemV1>? cashflowItems = null,
        ICollection<ClassicCityWorkplacePayrollSettlementItemV1>? workplacePayrollItems = null)
    {
        return HouseholdCashflowSettlementStep.ApplyAsync(
            cityId: TestCityId,
            householdsById: householdsById,
            residentsByHouseholdId: residentsByHouseholdId,
            housingByHouseholdId: housingByHouseholdId ?? new Dictionary<HouseholdId, HousingStatus>(),
            residentialBuildingIdByHouseholdId: residentialBuildingIdByHouseholdId ??
                                                new Dictionary<HouseholdId, ResidentialBuildingId?>(),
            previousDate: previousDate ?? PreviousDate,
            currentDate: currentDate ?? CurrentDate,
            householdCashflowPolicy: new CityHouseholdCashflowPolicy(),
            costOfLivingState: costOfLivingState,
            essentialsState: essentialsState,
            livingConditionsState: livingConditionsState,
            districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId ??
                                                   new Dictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>(),
            districtByHouseholdId: districtByHouseholdId ?? new Dictionary<HouseholdId, DistrictId?>(),
            districtImpactPolicy: new CityPopulationDistrictImpactPolicy(),
            participationPolicy: new CityPopulationParticipationPolicy(),
            commuteRoutingService: commuteRoutingService ?? new RecordingCommuteRoutingService(),
            cashflowItems: cashflowItems ?? new List<ClassicCityHouseholdCashflowSettlementItemV1>(),
            workplacePayrollItems: workplacePayrollItems ?? new List<ClassicCityWorkplacePayrollSettlementItemV1>(),
            cancellationToken: CancellationToken.None);
    }

    private static HouseholdEntity CreateHousehold(HouseholdId householdId)
    {
        return HouseholdEntity.Create(
            id: householdId,
            size: HouseholdSize.From(1),
            createdAtUtc: CreatedAtUtc,
            cashReserve: Money.FromDecimal(100m));
    }

    private static PersonEntity CreateRetiredResident(
        HouseholdId householdId,
        int personIndex)
    {
        return CreatePerson(
            personId: CreateGuid(prefix: "11111111", index: personIndex),
            householdId: householdId.Value,
            birthDate: new DateOnly(1960, 5, 4),
            currentDate: CurrentDate,
            employmentStatus: EmploymentStatus.Retired);
    }

    private static PersonEntity CreateEmployedResident(
        HouseholdId householdId,
        int personIndex,
        Job job)
    {
        return CreatePerson(
            personId: CreateGuid(prefix: "22222222", index: personIndex),
            householdId: householdId.Value,
            birthDate: new DateOnly(1990, 5, 4),
            currentDate: CurrentDate,
            employmentStatus: EmploymentStatus.Employed,
            job: job);
    }

    private static Job CreateJob(
        string title,
        int index)
    {
        return new Job(
            workplaceId: WorkplaceId.From(CreateGuid(prefix: "33333333", index: index)),
            title: title,
            workplaceAnchorId: CityAnchorId.From(CreateGuid(prefix: "44444444", index: index)));
    }

    private static HouseholdId CreateHouseholdId(int index)
    {
        return HouseholdId.From(CreateGuid(prefix: "55555555", index: index));
    }

    private static Guid CreateGuid(
        string prefix,
        int index)
    {
        return Guid.Parse($"{prefix}-0000-0000-0000-{index:000000000000}");
    }

    private static CityPopulationLivingConditionsContext CreateDefaultLivingConditions()
    {
        return new CityPopulationLivingConditionsContext(
            FloodingIndex: 0m,
            RoadAccessibilityIndex: 1m,
            PowerCoverageIndex: 1m,
            UtilityContinuityIndex: 1m,
            HeatingCoverageIndex: 1m,
            WaterCoverageIndex: 1m,
            SanitationCoverageIndex: 1m);
    }

    private static CityPopulationEssentialsContext CreateDefaultEssentials()
    {
        return new CityPopulationEssentialsContext(
            SupplyStressIndex: 1m,
            EmergencyRationingEnabled: false,
            FoodStockLevelIndex: 1m,
            FoodShortageRiskIndex: 0m,
            MedicineStockLevelIndex: 1m,
            MedicineShortageRiskIndex: 0m,
            EmergencyWaterStockLevelIndex: 1m,
            EmergencyWaterShortageRiskIndex: 0m);
    }

    private sealed class RecordingCommuteRoutingService : ICityPopulationCommuteRoutingService
    {
        public List<PersonEntity> EmploymentCommuteResidents { get; } = [];
        public List<ResidentialBuildingId?> EmploymentResidentialBuildingIds { get; } = [];
        public CityPopulationCommuteContext EmploymentContext { get; set; } = CityPopulationCommuteContext.Neutral;

        public Task PreloadAnchorCommutesAsync(
            Guid cityId,
            IReadOnlyCollection<CityPopulationCommuteRouteRequest> requests,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<CityPopulationCommuteContext> ResolveAnchorCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            CityAnchorId? destinationAnchorId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<CityPopulationCommuteContext> ResolveEmploymentCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            Person resident,
            CancellationToken cancellationToken)
        {
            EmploymentCommuteResidents.Add(resident);
            EmploymentResidentialBuildingIds.Add(residentialBuildingId);
            return Task.FromResult(EmploymentContext);
        }

        public Task<CityPopulationCommuteContext> ResolveEducationCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            Person resident,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<CityPopulationCommuteContext> ResolveHealthcareCommuteAsync(
            Guid cityId,
            ResidentialBuildingId? residentialBuildingId,
            CityAnchorId? healthcareAnchorId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
