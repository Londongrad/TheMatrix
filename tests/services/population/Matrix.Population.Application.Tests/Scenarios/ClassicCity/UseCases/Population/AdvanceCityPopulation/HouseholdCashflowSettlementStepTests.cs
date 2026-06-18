using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Economy;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation;
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

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    public sealed class HouseholdCashflowSettlementStepTests
    {
        private const int SettledDays = 3;
        private const decimal RetailTaxRate = 0.08m;
        private static readonly CityId TestCityId = CityId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

        private static readonly DateOnly PreviousDate = new(
            year: 2048,
            month: 5,
            day: 1);

        private static readonly DateOnly CurrentDate = new(
            year: 2048,
            month: 5,
            day: 4);

        private static readonly DateTimeOffset CreatedAtUtc = new(
            year: 2048,
            month: 1,
            day: 1,
            hour: 0,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        [Fact]
        public async Task ApplyAsync_WhenCurrentDateDoesNotAdvance_ReturnsNullAndDoesNotAddItems()
        {
            HouseholdId householdId = CreateHouseholdId(1);
            HouseholdEntity household = CreateHousehold(householdId);
            PersonEntity resident = CreateRetiredResident(
                householdId: householdId,
                personIndex: 1);
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
            Assert.Equal(
                expected: beforeCashReserve,
                actual: household.CashReserve);
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
            PersonEntity resident = CreateRetiredResident(
                householdId: householdId,
                personIndex: 1);
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
            Assert.Equal(
                expected: householdId.Value,
                actual: item.HouseholdId);
            Assert.Equal(
                expected: ClassicCityEconomySettlementBatchFactory.BuildHouseholdExternalReferenceCode(householdId),
                actual: item.ExternalReferenceCode);
            Assert.Equal(
                expected: expectedIncome.GrossIncome.Multiply(SettledDays)
                   .Amount,
                actual: item.GrossPayrollAmount);
            Assert.Equal(
                expected: expectedIncome.TaxWithheld.Multiply(SettledDays)
                   .Amount,
                actual: item.IncomeTaxAmount);
            Assert.Equal(
                expected: expectedIncome.NetIncome.Multiply(SettledDays)
                   .Amount,
                actual: item.NetPayrollAmount);
            Assert.Equal(
                expected: expectedCashflow.RetailTurnover.Multiply(SettledDays)
                   .Amount,
                actual: item.RetailTurnoverAmount);
            Assert.Equal(
                expected: expectedCashflow.RetailTurnover.Multiply(SettledDays)
                   .Multiply(RetailTaxRate)
                   .Amount,
                actual: item.RetailTaxAmount);
            Assert.Equal(
                expected: expectedCashflow.RetailStoreSpend.Multiply(SettledDays)
                   .Amount,
                actual: item.RetailStoreSpendAmount);
            Assert.Equal(
                expected: expectedCashflow.ServiceSpend.Multiply(SettledDays)
                   .Amount,
                actual: item.ServiceSpendAmount);
            Assert.Equal(
                expected: expectedCashflow.MunicipalSpend.Multiply(SettledDays)
                   .Amount,
                actual: item.MunicipalSpendAmount);
            Assert.Empty(workplacePayrollItems);

            Assert.NotNull(result);
            Assert.Equal(
                expected: CurrentDate,
                actual: result!.CurrentDate);
            Assert.Equal(
                expected: SettledDays,
                actual: result.SettledDays);
            Assert.Equal(
                expected: 1,
                actual: result.HouseholdCount);
            Assert.Equal(
                expected: expectedCashflow.ResidentCount,
                actual: result.ResidentCount);
            Assert.Equal(
                expected: expectedIncome.GrossIncome.Multiply(SettledDays),
                actual: result.GrossPayroll);
            Assert.Equal(
                expected: expectedIncome.TaxWithheld.Multiply(SettledDays),
                actual: result.IncomeTax);
            Assert.Equal(
                expected: expectedIncome.NetIncome.Multiply(SettledDays),
                actual: result.NetPayroll);
            Assert.Equal(
                expected: expectedCashflow.RetailTurnover.Multiply(SettledDays),
                actual: result.RetailTurnover);
            Assert.Equal(
                expected: expectedCashflow.RetailTurnover.Multiply(SettledDays)
                   .Multiply(RetailTaxRate),
                actual: result.RetailTax);
            Assert.Equal(
                expected: expectedCashflow.HousingExpense.Multiply(SettledDays),
                actual: result.HousingSpend);
        }

        [Fact]
        public async Task ApplyAsync_WhenResidentIsEmployedWithJob_AddsWorkplacePayrollItemAndUsesEmploymentCommute()
        {
            HouseholdId householdId = CreateHouseholdId(1);
            var residentialBuildingId = ResidentialBuildingId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
            Job job = CreateJob(
                title: "Engineer",
                index: 1);
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

            Assert.Equal(
                expected: [resident],
                actual: routingService.EmploymentCommuteResidents);
            ResidentialBuildingId? resolvedResidentialBuildingId =
                Assert.Single(routingService.EmploymentResidentialBuildingIds);
            Assert.Equal(
                expected: residentialBuildingId,
                actual: resolvedResidentialBuildingId);

            ClassicCityWorkplacePayrollSettlementItemV1 payroll = Assert.Single(workplacePayrollItems);
            Assert.Equal(
                expected: householdId.Value,
                actual: payroll.HouseholdId);
            Assert.Equal(
                expected: ClassicCityEconomySettlementBatchFactory.BuildHouseholdExternalReferenceCode(householdId),
                actual: payroll.HouseholdExternalReferenceCode);
            Assert.Equal(
                expected: job.WorkplaceId.Value,
                actual: payroll.WorkplaceId);
            Assert.Equal(
                expected: ClassicCityWorkplaceBusinessSyncBatchFactory.BuildExternalReferenceCode(job.WorkplaceId),
                actual: payroll.WorkplaceExternalReferenceCode);
            Assert.Equal(
                expected: job.Title,
                actual: payroll.JobTitle);
            Assert.Equal(
                expected: expectedIncome.GrossIncome.Multiply(SettledDays)
                   .Amount,
                actual: payroll.GrossPayrollAmount);
            Assert.Equal(
                expected: expectedIncome.TaxWithheld.Multiply(SettledDays)
                   .Amount,
                actual: payroll.IncomeTaxAmount);
            Assert.Equal(
                expected: expectedIncome.NetIncome.Multiply(SettledDays)
                   .Amount,
                actual: payroll.NetPayrollAmount);
            Assert.True(
                payroll.NetPayrollAmount <=
                neutralIncome.NetIncome.Multiply(SettledDays)
                   .Amount);

            ClassicCityHouseholdCashflowSettlementItemV1 cashflowItem = Assert.Single(cashflowItems);
            Assert.Equal(
                expected: 0m,
                actual: cashflowItem.GrossPayrollAmount);
            Assert.Equal(
                expected: 0m,
                actual: cashflowItem.IncomeTaxAmount);
            Assert.Equal(
                expected: 0m,
                actual: cashflowItem.NetPayrollAmount);
            Assert.NotNull(result);
            Assert.Equal(
                expected: Money.Zero,
                actual: result!.GrossPayroll);
            Assert.Equal(
                expected: Money.Zero,
                actual: result.IncomeTax);
            Assert.Equal(
                expected: Money.Zero,
                actual: result.NetPayroll);
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
            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>?
                districtUtilityConditionsByDistrictId = null,
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
                                                       new Dictionary<DistrictId,
                                                           CityDistrictUtilityConditionsSnapshot>(),
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
                personId: CreateGuid(
                    prefix: "11111111",
                    index: personIndex),
                householdId: householdId.Value,
                birthDate: new DateOnly(
                    year: 1960,
                    month: 5,
                    day: 4),
                currentDate: CurrentDate,
                employmentStatus: EmploymentStatus.Retired);
        }

        private static PersonEntity CreateEmployedResident(
            HouseholdId householdId,
            int personIndex,
            Job job)
        {
            return CreatePerson(
                personId: CreateGuid(
                    prefix: "22222222",
                    index: personIndex),
                householdId: householdId.Value,
                birthDate: new DateOnly(
                    year: 1990,
                    month: 5,
                    day: 4),
                currentDate: CurrentDate,
                employmentStatus: EmploymentStatus.Employed,
                job: job);
        }

        private static Job CreateJob(
            string title,
            int index)
        {
            return new Job(
                workplaceId: WorkplaceId.From(
                    CreateGuid(
                        prefix: "33333333",
                        index: index)),
                title: title,
                workplaceAnchorId: CityAnchorId.From(
                    CreateGuid(
                        prefix: "44444444",
                        index: index)));
        }

        private static HouseholdId CreateHouseholdId(int index)
        {
            return HouseholdId.From(
                CreateGuid(
                    prefix: "55555555",
                    index: index));
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
                PersonEntity resident,
                CancellationToken cancellationToken)
            {
                EmploymentCommuteResidents.Add(resident);
                EmploymentResidentialBuildingIds.Add(residentialBuildingId);
                return Task.FromResult(EmploymentContext);
            }

            public Task<CityPopulationCommuteContext> ResolveEducationCommuteAsync(
                Guid cityId,
                ResidentialBuildingId? residentialBuildingId,
                PersonEntity resident,
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
}
