using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Models;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetOperations.RunCityMunicipalOperatingCycle;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RunCityBusinessTaxCycle;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.Common;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdObligations.RunCityHouseholdBillingCycle;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Matrix.Economy.Domain.Services;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.Services
{
    public sealed class CityEconomyRecurringCycleExecutionService(
        ICityBudgetAllocationRepository allocationRepository,
        ICityBudgetRepository budgetRepository,
        ICityBusinessRepository businessRepository,
        ICityEconomyCostProfileStateRepository costProfileStateRepository,
        ICityHouseholdAccountRepository householdAccountRepository,
        ICityHouseholdObligationRepository obligationRepository,
        HouseholdObligationChargeSupport chargeSupport,
        CityBusinessTaxRemittanceSupport taxRemittanceSupport,
        CityEconomyCostProfilePolicy costProfilePolicy,
        CityEconomyServiceQualityPolicy serviceQualityPolicy,
        CityMunicipalOperatingCyclePolicy municipalOperatingCyclePolicy,
        CityBudgetBusinessDisbursementSupport disbursementSupport)
    {
        private const int FinancialStressBatchSize = 500;

        public async Task<ClassicCityCostOfLivingSnapshotV1?> ExecuteCostOfLivingAsync(
            Guid cityId,
            DateTimeOffset asOfUtc,
            CancellationToken cancellationToken)
        {
            CityEconomyCostProfileState? state = await costProfileStateRepository.GetByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            if (state is null)
                return null;

            CityBudget? budget = await budgetRepository.GetByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            IReadOnlyList<CityBudgetAllocation> allocations = await allocationRepository.ListByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            IReadOnlyList<CityBusiness> businesses = await businessRepository.ListByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            IReadOnlyList<CityHouseholdObligation> obligations = await obligationRepository.ListByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            CityEconomyCostProfileSnapshot snapshot = costProfilePolicy.Recalculate(
                state: state,
                budget: budget,
                allocations: allocations,
                businesses: businesses,
                asOfUtc: asOfUtc);

            state.ApplySnapshot(
                snapshot: snapshot,
                updatedAtUtc: asOfUtc);

            foreach (CityHouseholdObligation obligation in obligations.Where(x => x.IsActive))
                obligation.Reprice(snapshot.ResolveObligationPriceMultiplier(obligation.Kind));

            return new ClassicCityCostOfLivingSnapshotV1(
                CityId: cityId,
                WageMultiplier: snapshot.WageMultiplier,
                RetailPriceMultiplier: snapshot.RetailPriceMultiplier,
                HousingCostMultiplier: snapshot.HousingCostMultiplier,
                UtilityCostMultiplier: snapshot.UtilityCostMultiplier,
                CostOfLivingIndex: snapshot.CostOfLivingIndex,
                AffordabilityIndex: snapshot.AffordabilityIndex,
                OccurredAtUtc: snapshot.EvaluatedAtUtc);
        }

        public async Task<ClassicCityServiceQualitySnapshotV1> ExecuteServiceQualityAsync(
            Guid cityId,
            DateTimeOffset asOfUtc,
            CancellationToken cancellationToken)
        {
            CityBudget? budget = await budgetRepository.GetByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            IReadOnlyList<CityBudgetAllocation> allocations = await allocationRepository.ListByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            IReadOnlyList<CityBusiness> businesses = await businessRepository.ListByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            CityEconomyServiceQualitySnapshot snapshot = serviceQualityPolicy.Evaluate(
                budget: budget,
                allocations: allocations,
                businesses: businesses,
                asOfUtc: asOfUtc);

            return new ClassicCityServiceQualitySnapshotV1(
                CityId: cityId,
                HealthcareQualityIndex: snapshot.HealthcareQualityIndex,
                EducationQualityIndex: snapshot.EducationQualityIndex,
                HousingSupportIndex: snapshot.HousingSupportIndex,
                OccurredAtUtc: snapshot.EvaluatedAtUtc);
        }

        public async Task<CityEconomyBillingCycleExecutionResult> ExecuteBillingAsync(
            Guid cityId,
            DateTimeOffset asOfUtc,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityHouseholdObligation> dueObligations = await obligationRepository.ListDueByCityAsync(
                cityId: cityId,
                asOfUtc: asOfUtc,
                cancellationToken: cancellationToken);
            IReadOnlyList<CityHouseholdObligation> cityObligations = await obligationRepository.ListByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            decimal totalChargedAmount = 0m;
            decimal totalTaxAmount = 0m;
            int chargedObligations = 0;

            foreach (CityHouseholdObligation obligation in dueObligations)
            {
                HouseholdObligationChargeAttemptResult attempt = await chargeSupport.TryChargeAsync(
                    obligation: obligation,
                    description: "Recurring billing cycle.",
                    occurredAtUtc: asOfUtc,
                    cancellationToken: cancellationToken);

                if (!attempt.Succeeded)
                    continue;

                totalChargedAmount += attempt.ChargedAmount.Amount;
                totalTaxAmount += attempt.ChargedTaxAmount.Amount;
                chargedObligations += attempt.SettledInstallmentCount;
            }

            ClassicCityHouseholdFinancialStressBatchV1[] financialStressBatches =
                await BuildFinancialStressBatchesAsync(
                    cityId: cityId,
                    asOfUtc: asOfUtc,
                    cityObligations: cityObligations,
                    cancellationToken: cancellationToken);

            return new CityEconomyBillingCycleExecutionResult(
                Result: new RunCityHouseholdBillingCycleResultDto(
                    CityId: cityId,
                    AsOfUtc: asOfUtc.ToString("O"),
                    ChargedObligations: chargedObligations,
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
            IReadOnlyList<CityHouseholdObligation> cityObligations,
            CancellationToken cancellationToken)
        {
            CityHouseholdObligation[] activeObligations = cityObligations
               .Where(x => x.IsActive)
               .ToArray();

            if (activeObligations.Length == 0)
                return [];

            var items = new List<ClassicCityHouseholdFinancialStressItemV1>();

            foreach (IGrouping<Guid, CityHouseholdObligation> group in
                     activeObligations.GroupBy(x => x.HouseholdAccountId))
            {
                CityHouseholdAccount? account = await householdAccountRepository.GetByIdAsync(
                    householdAccountId: group.Key,
                    cancellationToken: cancellationToken);

                if (account is null || string.IsNullOrWhiteSpace(account.ExternalReferenceCode))
                    continue;

                CityHouseholdObligation[] obligations = group
                   .Where(x => x.IsActive && x.ResolveDueInstallmentCount(asOfUtc) > 0)
                   .ToArray();
                decimal overdueAmount = obligations.Sum(x => x.ResolveCurrentDueAmount(asOfUtc)
                   .Amount);
                int overdueRentCount = obligations.Count(x => x.Kind == CityHouseholdObligationKind.Rent);
                int overdueUtilityCount = obligations.Count(x => x.Kind == CityHouseholdObligationKind.Utilities);
                int arrearsObligationCount = obligations.Count(x =>
                    x.ResolveDelinquentBillingCycles(asOfUtc) >= 2 ||
                    x.ResolveDelinquencyAgeDays(asOfUtc) >= 30);
                int serviceCutoffCount = obligations.Count(x => x.HasServiceCutoff);
                int evictionNoticeCount = obligations.Count(x => x.HasEvictionNotice);
                int evictionEligibleCount = obligations.Count(x => x.IsEvictionEligible);
                int oldestOverdueAgeDays = obligations.Length == 0
                    ? 0
                    : obligations.Max(x => x.ResolveDelinquencyAgeDays(asOfUtc));
                decimal distressScore = Math.Clamp(
                    value: (obligations.Length * 0.18m) +
                           (overdueRentCount * 0.22m) +
                           (overdueUtilityCount * 0.12m) +
                           (arrearsObligationCount * 0.10m) +
                           (serviceCutoffCount * 0.18m) +
                           (evictionNoticeCount * 0.24m) +
                           (evictionEligibleCount * 0.30m) +
                           Math.Min(
                               val1: 0.24m,
                               val2: oldestOverdueAgeDays / 120m) +
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
                        ArrearsObligationCount: arrearsObligationCount,
                        ServiceCutoffCount: serviceCutoffCount,
                        EvictionNoticeCount: evictionNoticeCount,
                        EvictionEligibleCount: evictionEligibleCount,
                        OldestOverdueAgeDays: oldestOverdueAgeDays,
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
