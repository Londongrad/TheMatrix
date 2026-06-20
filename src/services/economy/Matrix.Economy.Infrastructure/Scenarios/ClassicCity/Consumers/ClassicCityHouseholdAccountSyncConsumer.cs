using MassTransit;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers;
using Microsoft.Extensions.Logging;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityHouseholdAccountSyncConsumer(
        ICityBudgetRepository budgetRepository,
        ICityBusinessRepository businessRepository,
        ICityEconomyCostProfileStateRepository costProfileStateRepository,
        ICityHouseholdAccountRepository householdAccountRepository,
        ICityHouseholdObligationRepository householdObligationRepository,
        ICityEconomyDeletionRepository deletionRepository,
        IEconomyUnitOfWork unitOfWork,
        ILogger<ClassicCityHouseholdAccountSyncConsumer> logger)
        : IConsumer<ClassicCityHouseholdAccountSyncBatchV1>
    {
        private const string DefaultLandlordTemplateKey = "city-housing-authority";
        private const string DefaultUtilityTemplateKey = "city-utilities-board";

        public async Task Consume(ConsumeContext<ClassicCityHouseholdAccountSyncBatchV1> context)
        {
            ClassicCityHouseholdAccountSyncBatchV1 message = context.Message;

            if (await deletionRepository.GetDeletedAtUtcAsync(
                    cityId: message.CityId,
                    cancellationToken: context.CancellationToken) is not null)
            {
                logger.LogDebug(
                    message: "Skipped household account sync for deleted cityId={CityId}, correlationId={CorrelationId}.",
                    message.CityId,
                    message.CorrelationId);
                return;
            }

            CityBudget budget = await CityBudgetInitializationSupport.EnsureExistsAsync(
                cityId: message.CityId,
                budgetRepository: budgetRepository,
                unitOfWork: unitOfWork,
                cancellationToken: context.CancellationToken);
            int createdAccounts = 0;
            int createdObligations = 0;
            var housedAccounts = new List<(CityHouseholdAccount Account, int MemberCount)>(message.Households.Count);

            foreach (ClassicCityHouseholdAccountSyncItemV1 household in message.Households)
            {
                CityHouseholdAccount? account = await householdAccountRepository.GetByCityAndExternalReferenceCodeAsync(
                    cityId: message.CityId,
                    externalReferenceCode: household.ExternalReferenceCode,
                    cancellationToken: context.CancellationToken);

                if (account is not null)
                    account.EnsureCompatibleUnit(budget.GetUnitProfile());
                else
                {
                    account = new CityHouseholdAccount(
                        id: Guid.NewGuid(),
                        cityId: message.CityId,
                        name: household.Name,
                        externalReferenceCode: household.ExternalReferenceCode,
                        createdAtUtc: household.CreatedAtUtc,
                        unitProfile: budget.GetUnitProfile(),
                        openingBalance: Money.FromDecimal(household.OpeningBalanceAmount));
                    householdAccountRepository.Add(account);
                    createdAccounts++;
                }

                if (household.IsHoused)
                    housedAccounts.Add((account, household.MemberCount));
            }

            createdObligations += await EnsureStarterObligationsAsync(
                cityId: message.CityId,
                occurredAtUtc: message.OccurredAtUtc,
                housedAccounts: housedAccounts,
                cancellationToken: context.CancellationToken);

            if (createdAccounts == 0 && createdObligations == 0)
            {
                logger.LogDebug(
                    message:
                    "Skipped classic city household account sync for cityId={CityId}, correlationId={CorrelationId}, batch={BatchNumber}/{TotalBatches}; all accounts already exist.",
                    message.CityId,
                    message.CorrelationId,
                    message.BatchNumber,
                    message.TotalBatches);
                return;
            }

            await unitOfWork.SaveChangesAsync(context.CancellationToken);

            logger.LogInformation(
                message:
                "Applied classic city household account sync for cityId={CityId}, correlationId={CorrelationId}, batch={BatchNumber}/{TotalBatches}, createdAccounts={CreatedAccounts}, createdObligations={CreatedObligations}.",
                message.CityId,
                message.CorrelationId,
                message.BatchNumber,
                message.TotalBatches,
                createdAccounts,
                createdObligations);
        }

        private async Task<int> EnsureStarterObligationsAsync(
            Guid cityId,
            DateTimeOffset occurredAtUtc,
            IReadOnlyList<(CityHouseholdAccount Account, int MemberCount)> housedAccounts,
            CancellationToken cancellationToken)
        {
            if (housedAccounts.Count == 0)
                return 0;

            CityBusiness? landlord = await businessRepository.GetByCityAndTemplateKeyAsync(
                cityId: cityId,
                templateKey: DefaultLandlordTemplateKey,
                cancellationToken: cancellationToken);
            CityBusiness? utility = await businessRepository.GetByCityAndTemplateKeyAsync(
                cityId: cityId,
                templateKey: DefaultUtilityTemplateKey,
                cancellationToken: cancellationToken);
            CityEconomyCostProfileState? costProfileState = await costProfileStateRepository.GetByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            CityEconomyCostProfileSnapshot costProfile = costProfileState?.ToSnapshot() ??
                                                         CityEconomyCostProfileSnapshot.Neutral(occurredAtUtc);

            if (landlord is null && utility is null)
                return 0;

            Guid[] householdIds = housedAccounts.Select(x => x.Account.Id)
               .ToArray();
            IReadOnlyList<CityHouseholdObligation> existingObligations =
                await householdObligationRepository.ListByHouseholdsAsync(
                    householdAccountIds: householdIds,
                    cancellationToken: cancellationToken);
            ILookup<Guid, CityHouseholdObligation> obligationsByHouseholdId =
                existingObligations.ToLookup(x => x.HouseholdAccountId);
            int created = 0;

            foreach ((CityHouseholdAccount account, int memberCount) in housedAccounts)
            {
                IEnumerable<CityHouseholdObligation> householdObligations = obligationsByHouseholdId[account.Id];

                if (landlord is not null &&
                    !householdObligations.Any(x
                        => x.ProviderBusinessId == landlord.Id && x.Kind == CityHouseholdObligationKind.Rent))
                {
                    landlord.EnsureCompatibleUnit(account.GetUnitProfile());
                    landlord.EnsureCanServeObligation(CityHouseholdObligationKind.Rent);
                    householdObligationRepository.Add(
                        CreateStarterObligation(
                            cityId: cityId,
                            account: account,
                            provider: landlord,
                            memberCount: memberCount,
                            kind: CityHouseholdObligationKind.Rent,
                            costProfile: costProfile,
                            createdAtUtc: occurredAtUtc));
                    created++;
                }

                if (utility is not null &&
                    !householdObligations.Any(x
                        => x.ProviderBusinessId == utility.Id && x.Kind == CityHouseholdObligationKind.Utilities))
                {
                    utility.EnsureCompatibleUnit(account.GetUnitProfile());
                    utility.EnsureCanServeObligation(CityHouseholdObligationKind.Utilities);
                    householdObligationRepository.Add(
                        CreateStarterObligation(
                            cityId: cityId,
                            account: account,
                            provider: utility,
                            memberCount: memberCount,
                            kind: CityHouseholdObligationKind.Utilities,
                            costProfile: costProfile,
                            createdAtUtc: occurredAtUtc));
                    created++;
                }
            }

            return created;
        }

        private static CityHouseholdObligation CreateStarterObligation(
            Guid cityId,
            CityHouseholdAccount account,
            CityBusiness provider,
            int memberCount,
            CityHouseholdObligationKind kind,
            CityEconomyCostProfileSnapshot costProfile,
            DateTimeOffset createdAtUtc)
        {
            memberCount = Math.Max(
                val1: 1,
                val2: memberCount);
            decimal chargeAmount = kind switch
            {
                CityHouseholdObligationKind.Rent => 96m +
                                                    (memberCount * 26m) +
                                                    (Math.Max(
                                                         val1: 0,
                                                         val2: memberCount - 3) *
                                                     18m),
                CityHouseholdObligationKind.Utilities => 18m +
                                                         (memberCount * 9m) +
                                                         (Math.Max(
                                                              val1: 0,
                                                              val2: memberCount - 2) *
                                                          4m),
                _ => 24m + (memberCount * 8m)
            };
            decimal taxAmount = kind == CityHouseholdObligationKind.Utilities
                ? decimal.Round(
                    d: chargeAmount * 0.08m,
                    decimals: 2,
                    mode: MidpointRounding.AwayFromZero)
                : 0m;
            string name = kind switch
            {
                CityHouseholdObligationKind.Rent => "Starter housing rent",
                CityHouseholdObligationKind.Utilities => "Starter utility service",
                _ => "Starter household obligation"
            };
            decimal priceMultiplier = costProfile.ResolveObligationPriceMultiplier(kind);

            var obligation = new CityHouseholdObligation(
                id: Guid.NewGuid(),
                cityId: cityId,
                householdAccountId: account.Id,
                providerBusinessId: provider.Id,
                name: name,
                kind: kind,
                billingCadence: CityHouseholdObligationBillingCadence.Monthly,
                createdAtUtc: createdAtUtc,
                firstChargeDueAtUtc: createdAtUtc.AddMonths(1),
                unitProfile: account.GetUnitProfile(),
                chargeAmount: Money.FromDecimal(chargeAmount),
                taxAmount: Money.FromDecimal(taxAmount));

            obligation.Reprice(priceMultiplier);
            return obligation;
        }
    }
}
