using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.BudgetOperations.Common;
using Matrix.Economy.Application.UseCases.BudgetOperations.RunCityMunicipalOperatingCycle;
using Matrix.Economy.Application.UseCases.Businesses.Common;
using Matrix.Economy.Application.UseCases.Businesses.RunCityBusinessTaxCycle;
using Matrix.Economy.Application.UseCases.HouseholdObligations.Common;
using Matrix.Economy.Application.UseCases.HouseholdObligations.RunCityHouseholdBillingCycle;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.Services;

namespace Matrix.Economy.Application.UseCases.Simulation.Common
{
    public sealed class CityEconomyRecurringCycleExecutionService(
        ICityBudgetAllocationRepository allocationRepository,
        ICityBusinessRepository businessRepository,
        ICityHouseholdAccountRepository householdAccountRepository,
        ICityHouseholdObligationRepository obligationRepository,
        HouseholdObligationChargeSupport chargeSupport,
        CityBusinessTaxRemittanceSupport taxRemittanceSupport,
        CityMunicipalOperatingCyclePolicy municipalOperatingCyclePolicy,
        CityBudgetBusinessDisbursementSupport disbursementSupport)
    {
        private const int FinancialStressBatchSize = 500;

        public async Task<CityEconomyBillingCycleExecutionResult> ExecuteBillingAsync(
            Guid cityId,
            DateTimeOffset asOfUtc,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityHouseholdObligation> dueObligations = await obligationRepository.ListDueByCityAsync(
                cityId: cityId,
                asOfUtc: asOfUtc,
                cancellationToken: cancellationToken);

            decimal totalChargedAmount = 0m;
            decimal totalTaxAmount = 0m;

            foreach (CityHouseholdObligation obligation in dueObligations)
            {
                HouseholdObligationChargeAttemptResult attempt = await chargeSupport.TryChargeAsync(
                    obligation: obligation,
                    description: "Recurring billing cycle.",
                    cancellationToken: cancellationToken);

                if (!attempt.Succeeded)
                    continue;

                totalChargedAmount += obligation.ChargeAmount.Amount;
                totalTaxAmount += obligation.TaxAmount.Amount;
            }

            ClassicCityHouseholdFinancialStressBatchV1[] financialStressBatches =
                await BuildFinancialStressBatchesAsync(
                    cityId: cityId,
                    asOfUtc: asOfUtc,
                    dueObligations: dueObligations,
                    cancellationToken: cancellationToken);

            return new CityEconomyBillingCycleExecutionResult(
                Result: new RunCityHouseholdBillingCycleResultDto(
                    CityId: cityId,
                    AsOfUtc: asOfUtc.ToString("O"),
                    ChargedObligations: dueObligations.Count,
                    TotalChargedAmount: totalChargedAmount,
                    TotalTaxAmount: totalTaxAmount),
                FinancialStressBatches: financialStressBatches);
        }

        public async Task<RunCityBusinessTaxCycleResultDto> ExecuteTaxCycleAsync(
            Guid cityId,
            CityBudgetCategory budgetCategory,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityBusiness> businesses = await businessRepository.ListByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            int remittedBusinesses = 0;
            decimal totalRemittedAmount = 0m;

            foreach (CityBusiness business in businesses.Where(x => x.TaxReserve.IsPositive))
            {
                decimal remittanceAmount = business.TaxReserve.Amount;

                await taxRemittanceSupport.RemitAsync(
                    business: business,
                    amount: business.TaxReserve,
                    budgetCategory: budgetCategory,
                    title: $"{business.Name} scheduled tax remittance",
                    description: "Recurring city business tax cycle.",
                    cancellationToken: cancellationToken);

                remittedBusinesses++;
                totalRemittedAmount += remittanceAmount;
            }

            return new RunCityBusinessTaxCycleResultDto(
                CityId: cityId,
                BudgetCategory: budgetCategory.ToString(),
                RemittedBusinesses: remittedBusinesses,
                TotalRemittedAmount: totalRemittedAmount);
        }

        public async Task<RunCityMunicipalOperatingCycleResultDto> ExecuteMunicipalOperatingCycleAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityBudgetAllocation> allocations = await allocationRepository.ListByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            IReadOnlyList<CityBusiness> businesses = await businessRepository.ListByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            int allocationCategoriesTouched = 0;
            int providerPayments = 0;
            decimal totalDisbursedAmount = 0m;

            foreach (CityBudgetAllocation allocation in allocations)
            {
                IReadOnlyList<CityMunicipalOperatingDisbursementDecision> decisions =
                    municipalOperatingCyclePolicy.BuildDisbursements(
                        allocation: allocation,
                        businesses: businesses);
                if (decisions.Count == 0)
                    continue;

                allocationCategoriesTouched++;

                foreach (CityMunicipalOperatingDisbursementDecision decision in decisions)
                {
                    CityBusiness? business = businesses.FirstOrDefault(x => x.Id == decision.BusinessId);
                    if (business is null)
                        continue;

                    await disbursementSupport.DisburseAsync(
                        business: business,
                        category: allocation.Category,
                        amount: decision.Amount,
                        title: $"{allocation.Category} operating disbursement",
                        description: "Recurring municipal operating cycle.",
                        cancellationToken: cancellationToken);

                    providerPayments++;
                    totalDisbursedAmount += decision.Amount;
                }
            }

            return new RunCityMunicipalOperatingCycleResultDto(
                CityId: cityId,
                AllocationCategoriesTouched: allocationCategoriesTouched,
                ProviderPayments: providerPayments,
                TotalDisbursedAmount: totalDisbursedAmount);
        }

        private async Task<ClassicCityHouseholdFinancialStressBatchV1[]> BuildFinancialStressBatchesAsync(
            Guid cityId,
            DateTimeOffset asOfUtc,
            IReadOnlyList<CityHouseholdObligation> dueObligations,
            CancellationToken cancellationToken)
        {
            if (dueObligations.Count == 0)
                return [];

            var items = new List<ClassicCityHouseholdFinancialStressItemV1>();

            foreach (IGrouping<Guid, CityHouseholdObligation> group in
                     dueObligations.GroupBy(x => x.HouseholdAccountId))
            {
                CityHouseholdAccount? account = await householdAccountRepository.GetByIdAsync(
                    householdAccountId: group.Key,
                    cancellationToken: cancellationToken);

                if (account is null || string.IsNullOrWhiteSpace(account.ExternalReferenceCode))
                    continue;

                CityHouseholdObligation[] obligations = group
                   .Where(x => x.IsActive && x.NextChargeDueAtUtc <= asOfUtc)
                   .ToArray();
                decimal overdueAmount = obligations.Sum(x => x.ChargeAmount.Amount);
                int overdueRentCount = obligations.Count(x => x.Kind == CityHouseholdObligationKind.Rent);
                int overdueUtilityCount = obligations.Count(x => x.Kind == CityHouseholdObligationKind.Utilities);
                decimal distressScore = Math.Clamp(
                    value: (obligations.Length * 0.18m) +
                           (overdueRentCount * 0.22m) +
                           (overdueUtilityCount * 0.12m) +
                           Math.Min(
                               val1: 0.40m,
                               val2: overdueAmount / 1000m),
                    min: 0m,
                    max: 1m);

                items.Add(
                    new ClassicCityHouseholdFinancialStressItemV1(
                        HouseholdAccountId: account.Id,
                        HouseholdExternalReferenceCode: account.ExternalReferenceCode,
                        OverdueObligationCount: obligations.Length,
                        OverdueRentCount: overdueRentCount,
                        OverdueUtilityCount: overdueUtilityCount,
                        TotalOverdueAmount: overdueAmount,
                        DistressScore: decimal.Round(
                            d: distressScore,
                            decimals: 4,
                            mode: MidpointRounding.AwayFromZero)));
            }

            if (items.Count == 0)
                return [];

            string correlationId = $"classic-city:{cityId:N}:household-financial-stress:{Guid.NewGuid():N}";
            ClassicCityHouseholdFinancialStressBatchV1[] batches = items
               .Chunk(FinancialStressBatchSize)
               .Select((
                    chunk,
                    index) => new ClassicCityHouseholdFinancialStressBatchV1(
                    CityId: cityId,
                    BatchNumber: index + 1,
                    TotalBatches: 0,
                    Households: chunk,
                    CorrelationId: correlationId,
                    OccurredAtUtc: asOfUtc))
               .ToArray();

            for (int i = 0; i < batches.Length; i++)
                batches[i] = batches[i] with
                {
                    TotalBatches = batches.Length
                };

            return batches;
        }
    }
}
