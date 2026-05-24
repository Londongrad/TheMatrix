using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Population.Application.Scenarios.ClassicCity.Common;
using Matrix.Population.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using HouseholdEntity = Matrix.Population.Domain.Entities.Household;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class HouseholdCashflowSettlementStep
    {
        internal static async Task<CityEconomyDailySettlementSnapshot?> ApplyAsync(
            CityId cityId,
            IReadOnlyDictionary<HouseholdId, HouseholdEntity> householdsById,
            IReadOnlyDictionary<HouseholdId, IReadOnlyCollection<PersonEntity>> residentsByHouseholdId,
            IReadOnlyDictionary<HouseholdId, HousingStatus> housingByHouseholdId,
            IReadOnlyDictionary<HouseholdId, ResidentialBuildingId?> residentialBuildingIdByHouseholdId,
            DateOnly previousDate,
            DateOnly currentDate,
            CityHouseholdCashflowPolicy householdCashflowPolicy,
            CityPopulationCostOfLivingState? costOfLivingState,
            CityPopulationEssentialsState? essentialsState,
            CityPopulationLivingConditionsState? livingConditionsState,
            IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot> districtUtilityConditionsByDistrictId,
            IReadOnlyDictionary<HouseholdId, DistrictId?> districtByHouseholdId,
            CityPopulationDistrictImpactPolicy districtImpactPolicy,
            CityPopulationParticipationPolicy participationPolicy,
            ICityPopulationCommuteRoutingService commuteRoutingService,
            ICollection<ClassicCityHouseholdCashflowSettlementItemV1> cashflowItems,
            ICollection<ClassicCityWorkplacePayrollSettlementItemV1> workplacePayrollItems,
            CancellationToken cancellationToken)
        {
            int daysElapsed = Math.Max(
                val1: 0,
                val2: currentDate.DayNumber - previousDate.DayNumber);
            if (daysElapsed <= 0)
                return null;

            Money grossPayroll = Money.Zero;
            Money incomeTax = Money.Zero;
            Money netPayroll = Money.Zero;
            Money retailTurnover = Money.Zero;
            Money retailTax = Money.Zero;
            Money housingSpend = Money.Zero;
            int settledHouseholdCount = 0;
            int settledResidentCount = 0;

            foreach ((HouseholdId householdId, HouseholdEntity household) in householdsById)
            {
                if (!residentsByHouseholdId.TryGetValue(
                        key: householdId,
                        value: out IReadOnlyCollection<PersonEntity>? residents) ||
                    residents.Count == 0)
                    continue;

                HousingStatus? housingStatus = housingByHouseholdId.TryGetValue(
                    key: householdId,
                    value: out HousingStatus resolvedHousingStatus)
                    ? resolvedHousingStatus
                    : null;
                CityHouseholdCashflowProfile cashflow = householdCashflowPolicy.Build(
                    householdResidents: residents,
                    housingStatus: housingStatus,
                    currentDate: currentDate,
                    costOfLivingState: costOfLivingState);

                Money retailTurnoverForPeriod = cashflow.RetailTurnover.Multiply(daysElapsed);
                retailTurnover = retailTurnover.Add(retailTurnoverForPeriod);
                Money retailTaxForPeriod = retailTurnoverForPeriod.Multiply(ResolveRetailTaxRate());
                retailTax = retailTax.Add(retailTaxForPeriod);
                housingSpend = housingSpend.Add(cashflow.HousingExpense.Multiply(daysElapsed));
                settledHouseholdCount++;
                settledResidentCount += cashflow.ResidentCount;

                Money supportGrossIncomeForPeriod = Money.Zero;
                Money supportIncomeTaxForPeriod = Money.Zero;
                Money supportNetIncomeForPeriod = Money.Zero;
                Money actualHouseholdNetIncomeForPeriod = Money.Zero;

                foreach (PersonEntity resident in residents)
                {
                    decimal incomeMultiplier = 1m;
                    DistrictId? districtId = districtByHouseholdId.TryGetValue(
                        key: resident.HouseholdId,
                        value: out DistrictId? resolvedDistrictId)
                        ? resolvedDistrictId
                        : null;
                    CityPopulationLivingConditionsContext districtLivingConditions = districtImpactPolicy.ResolveLivingConditions(
                        districtId: districtId,
                        livingConditionsState: livingConditionsState,
                        districtUtilityConditions: ClassicCityHousingOpportunityPlanner.ResolveDistrictUtilityConditions(
                            districtId: districtId,
                            districtUtilityConditionsByDistrictId: districtUtilityConditionsByDistrictId));
                    CityPopulationEssentialsContext districtEssentials = districtImpactPolicy.ResolveEssentials(
                        districtId: districtId,
                        essentialsState: essentialsState);
                    if (resident.Employment.Status == EmploymentStatus.Employed)
                    {
                        HousingStatus? residentHousingStatus = housingStatus;
                        ResidentialBuildingId? residentialBuildingId =
                            residentialBuildingIdByHouseholdId.TryGetValue(
                                key: resident.HouseholdId,
                                value: out ResidentialBuildingId? resolvedResidentialBuildingId)
                                ? resolvedResidentialBuildingId
                                : null;
                        CityPopulationCommuteContext employmentCommute =
                            await commuteRoutingService.ResolveEmploymentCommuteAsync(
                                cityId: cityId.Value,
                                residentialBuildingId: residentialBuildingId,
                                resident: resident,
                                cancellationToken: cancellationToken);
                        CityPopulationParticipationProfile employmentProfile =
                            participationPolicy.ResolveEmploymentProfile(
                                person: resident,
                                currentDate: currentDate,
                                housingStatus: residentHousingStatus,
                                livingConditions: districtLivingConditions,
                                essentials: districtEssentials,
                                commute: employmentCommute);
                        incomeMultiplier = employmentProfile.PayrollMultiplier;
                    }

                    CityResidentIncomeSettlementProfile residentIncome = householdCashflowPolicy.BuildResidentIncome(
                        resident: resident,
                        currentDate: currentDate,
                        costOfLivingState: costOfLivingState,
                        incomeMultiplier: incomeMultiplier);
                    Money residentGrossIncomeForPeriod = residentIncome.GrossIncome.Multiply(daysElapsed);
                    Money residentTaxForPeriod = residentIncome.TaxWithheld.Multiply(daysElapsed);
                    Money residentNetIncomeForPeriod = residentIncome.NetIncome.Multiply(daysElapsed);
                    actualHouseholdNetIncomeForPeriod = actualHouseholdNetIncomeForPeriod.Add(residentNetIncomeForPeriod);

                    if (resident.Employment.Status == EmploymentStatus.Employed &&
                        resident.Employment.Job is
                            { } job &&
                        residentNetIncomeForPeriod.IsPositive)
                    {
                        workplacePayrollItems.Add(
                            new ClassicCityWorkplacePayrollSettlementItemV1(
                                HouseholdId: householdId.Value,
                                HouseholdExternalReferenceCode: ClassicCityEconomySettlementBatchFactory
                                   .BuildHouseholdExternalReferenceCode(householdId),
                                WorkplaceId: job.WorkplaceId.Value,
                                WorkplaceExternalReferenceCode: ClassicCityWorkplaceBusinessSyncBatchFactory
                                   .BuildExternalReferenceCode(job.WorkplaceId),
                                JobTitle: job.Title,
                                GrossPayrollAmount: residentGrossIncomeForPeriod.Amount,
                                IncomeTaxAmount: residentTaxForPeriod.Amount,
                                NetPayrollAmount: residentNetIncomeForPeriod.Amount));
                        continue;
                    }

                    supportGrossIncomeForPeriod = supportGrossIncomeForPeriod.Add(residentGrossIncomeForPeriod);
                    supportIncomeTaxForPeriod = supportIncomeTaxForPeriod.Add(residentTaxForPeriod);
                    supportNetIncomeForPeriod = supportNetIncomeForPeriod.Add(residentNetIncomeForPeriod);
                }

                if (supportNetIncomeForPeriod.IsPositive || retailTurnoverForPeriod.IsPositive)
                    cashflowItems.Add(
                        new ClassicCityHouseholdCashflowSettlementItemV1(
                            HouseholdId: householdId.Value,
                            ExternalReferenceCode: ClassicCityEconomySettlementBatchFactory
                               .BuildHouseholdExternalReferenceCode(householdId),
                            GrossPayrollAmount: supportGrossIncomeForPeriod.Amount,
                            IncomeTaxAmount: supportIncomeTaxForPeriod.Amount,
                            NetPayrollAmount: supportNetIncomeForPeriod.Amount,
                            RetailTurnoverAmount: retailTurnoverForPeriod.Amount,
                            RetailTaxAmount: retailTaxForPeriod.Amount,
                            RetailStoreSpendAmount: cashflow.RetailStoreSpend.Multiply(daysElapsed).Amount,
                            ServiceSpendAmount: cashflow.ServiceSpend.Multiply(daysElapsed).Amount,
                            MunicipalSpendAmount: cashflow.MunicipalSpend.Multiply(daysElapsed).Amount));

                grossPayroll = grossPayroll.Add(supportGrossIncomeForPeriod);
                incomeTax = incomeTax.Add(supportIncomeTaxForPeriod);
                netPayroll = netPayroll.Add(supportNetIncomeForPeriod);

                household.ApplyDailyCashflow(
                    takeHomeIncome: Money.FromDecimal(actualHouseholdNetIncomeForPeriod.Amount / daysElapsed),
                    expenses: cashflow.DailyExpenses,
                    daysElapsed: daysElapsed);
            }

            return settledHouseholdCount == 0
                ? null
                : new CityEconomyDailySettlementSnapshot(
                    CurrentDate: currentDate,
                    SettledDays: daysElapsed,
                    HouseholdCount: settledHouseholdCount,
                    ResidentCount: settledResidentCount,
                    GrossPayroll: grossPayroll,
                    IncomeTax: incomeTax,
                    NetPayroll: netPayroll,
                    RetailTurnover: retailTurnover,
                    RetailTax: retailTax,
                    HousingSpend: housingSpend);
        }

        private static decimal ResolveRetailTaxRate()
        {
            return 0.08m;
        }
    }
}
