using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Matrix.Economy.Infrastructure.Consumers
{
    public sealed class ClassicCityHouseholdCashflowSettlementConsumer(
        ICityBusinessRepository businessRepository,
        ICityBusinessLedgerRepository businessLedgerRepository,
        ICityHouseholdAccountRepository householdAccountRepository,
        ICityHouseholdAccountLedgerRepository householdLedgerRepository,
        IEconomyUnitOfWork unitOfWork,
        ILogger<ClassicCityHouseholdCashflowSettlementConsumer> logger)
        : IConsumer<ClassicCityHouseholdCashflowSettlementBatchV1>
    {
        public async Task Consume(ConsumeContext<ClassicCityHouseholdCashflowSettlementBatchV1> context)
        {
            ClassicCityHouseholdCashflowSettlementBatchV1 message = context.Message;
            CityBusiness? retailProvider = await ResolveRetailProviderAsync(
                cityId: message.CityId,
                cancellationToken: context.CancellationToken);
            int settledPayrollAccounts = 0;
            int settledRetailAccounts = 0;

            foreach (ClassicCityHouseholdCashflowSettlementItemV1 household in message.Households)
            {
                CityHouseholdAccount? account = await householdAccountRepository.GetByCityAndExternalReferenceCodeAsync(
                    cityId: message.CityId,
                    externalReferenceCode: household.ExternalReferenceCode,
                    cancellationToken: context.CancellationToken);

                if (account is null)
                {
                    logger.LogWarning(
                        message:
                        "Skipped household cashflow settlement for cityId={CityId}, correlationId={CorrelationId}, householdRef={HouseholdRef}; account was not found.",
                        message.CityId,
                        message.CorrelationId,
                        household.ExternalReferenceCode);
                    continue;
                }

                string payrollReferenceCode = BuildReferenceCode(
                    correlationId: message.CorrelationId,
                    householdId: household.HouseholdId,
                    segment: "payroll");
                if (household.NetPayrollAmount > 0m &&
                    !await householdLedgerRepository.ExistsAsync(
                        householdAccountId: account.Id,
                        kind: CityHouseholdAccountLedgerEntryKind.PayrollIncome,
                        referenceCode: payrollReferenceCode,
                        cancellationToken: context.CancellationToken))
                {
                    var netPayroll = Money.FromDecimal(household.NetPayrollAmount);
                    account.ReceivePayroll(netPayroll);
                    await householdLedgerRepository.AddAsync(
                        entry: new CityHouseholdAccountLedgerEntry(
                            id: Guid.NewGuid(),
                            householdAccountId: account.Id,
                            cityId: account.CityId,
                            occurredAtUtc: message.OccurredAtUtc,
                            kind: CityHouseholdAccountLedgerEntryKind.PayrollIncome,
                            amount: netPayroll,
                            title: "Daily household payroll settlement",
                            description:
                            $"Settled {message.SettledDays} day(s) of take-home payroll into the economy account.",
                            source: CityHouseholdAccountLedgerEntrySource.Settlement,
                            referenceCode: payrollReferenceCode),
                        cancellationToken: context.CancellationToken);
                    settledPayrollAccounts++;
                }

                if (retailProvider is null || household.RetailTurnoverAmount <= 0m)
                    continue;

                string retailReferenceCode = BuildReferenceCode(
                    correlationId: message.CorrelationId,
                    householdId: household.HouseholdId,
                    segment: "retail");
                bool householdRetailEntryExists = await householdLedgerRepository.ExistsAsync(
                    householdAccountId: account.Id,
                    kind: CityHouseholdAccountLedgerEntryKind.ConsumerPurchase,
                    referenceCode: retailReferenceCode,
                    cancellationToken: context.CancellationToken);
                bool businessRetailEntryExists = await businessLedgerRepository.ExistsAsync(
                    businessId: retailProvider.Id,
                    kind: CityBusinessLedgerEntryKind.RetailSale,
                    referenceCode: retailReferenceCode,
                    cancellationToken: context.CancellationToken);

                if (householdRetailEntryExists || businessRetailEntryExists)
                    continue;

                retailProvider.EnsureCompatibleUnit(account.GetUnitProfile());
                retailProvider.EnsureCanRecordConsumerSale();

                var grossRetail = Money.FromDecimal(household.RetailTurnoverAmount);
                var retailTax = Money.FromDecimal(household.RetailTaxAmount);

                account.RecordConsumerPurchase(grossRetail);
                retailProvider.RecordSettledRetailSale(
                    grossAmount: grossRetail,
                    salesTaxAmount: retailTax);

                await householdLedgerRepository.AddAsync(
                    entry: new CityHouseholdAccountLedgerEntry(
                        id: Guid.NewGuid(),
                        householdAccountId: account.Id,
                        cityId: account.CityId,
                        occurredAtUtc: message.OccurredAtUtc,
                        kind: CityHouseholdAccountLedgerEntryKind.ConsumerPurchase,
                        amount: grossRetail,
                        title: "Daily household retail settlement",
                        description:
                        $"Settled {message.SettledDays} day(s) of household consumer spending into the economy ledger.",
                        source: CityHouseholdAccountLedgerEntrySource.Settlement,
                        referenceCode: retailReferenceCode),
                    cancellationToken: context.CancellationToken);
                await businessLedgerRepository.AddAsync(
                    entry: new CityBusinessLedgerEntry(
                        id: Guid.NewGuid(),
                        businessId: retailProvider.Id,
                        cityId: retailProvider.CityId,
                        occurredAtUtc: message.OccurredAtUtc,
                        kind: CityBusinessLedgerEntryKind.RetailSale,
                        amount: grossRetail,
                        taxAmount: retailTax,
                        title: "Daily household retail settlement",
                        description:
                        "Retail turnover settled from classic city household cashflow. Sales tax was already transferred through aggregate city settlement.",
                        source: CityBusinessLedgerEntrySource.Settlement,
                        referenceCode: retailReferenceCode),
                    cancellationToken: context.CancellationToken);
                settledRetailAccounts++;
            }

            if (settledPayrollAccounts == 0 && settledRetailAccounts == 0)
            {
                logger.LogDebug(
                    message:
                    "Skipped duplicate classic city household cashflow settlement for cityId={CityId}, correlationId={CorrelationId}, batch={BatchNumber}/{TotalBatches}.",
                    message.CityId,
                    message.CorrelationId,
                    message.BatchNumber,
                    message.TotalBatches);
                return;
            }

            await unitOfWork.SaveChangesAsync(context.CancellationToken);

            logger.LogInformation(
                message:
                "Applied classic city household cashflow settlement for cityId={CityId}, correlationId={CorrelationId}, batch={BatchNumber}/{TotalBatches}, settledPayrollAccounts={SettledPayrollAccounts}, settledRetailAccounts={SettledRetailAccounts}.",
                message.CityId,
                message.CorrelationId,
                message.BatchNumber,
                message.TotalBatches,
                settledPayrollAccounts,
                settledRetailAccounts);
        }

        private async Task<CityBusiness?> ResolveRetailProviderAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CityBusiness> businesses = await businessRepository.ListByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            return businesses
               .OrderBy(x => GetRetailPriority(x.Kind))
               .ThenBy(x => x.Name)
               .FirstOrDefault(x => GetRetailPriority(x.Kind) < int.MaxValue);
        }

        private static int GetRetailPriority(CityBusinessKind kind)
        {
            return kind switch
            {
                CityBusinessKind.RetailStore => 0,
                CityBusinessKind.Service => 1,
                CityBusinessKind.MunicipalVendor => 2,
                CityBusinessKind.Generic => 3,
                CityBusinessKind.Utility => 4,
                _ => int.MaxValue
            };
        }

        private static string BuildReferenceCode(
            string correlationId,
            Guid householdId,
            string segment)
        {
            return $"{correlationId}:household:{householdId:N}:{segment}";
        }
    }
}
