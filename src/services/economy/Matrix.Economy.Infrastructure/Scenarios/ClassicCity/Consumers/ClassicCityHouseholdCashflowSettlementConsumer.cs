using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Microsoft.Extensions.Logging;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityHouseholdCashflowSettlementConsumer(
        ICityBusinessRepository businessRepository,
        ICityBusinessLedgerRepository businessLedgerRepository,
        ICityHouseholdAccountRepository householdAccountRepository,
        ICityHouseholdAccountLedgerRepository householdLedgerRepository,
        CityHouseholdConsumerSpendAllocationPolicy householdConsumerSpendAllocationPolicy,
        ICityEconomyDeletionRepository deletionRepository,
        IEconomyUnitOfWork unitOfWork,
        ILogger<ClassicCityHouseholdCashflowSettlementConsumer> logger)
        : IConsumer<ClassicCityHouseholdCashflowSettlementBatchV1>
    {
        public async Task Consume(ConsumeContext<ClassicCityHouseholdCashflowSettlementBatchV1> context)
        {
            ClassicCityHouseholdCashflowSettlementBatchV1 message = context.Message;

            if (await deletionRepository.GetDeletedAtUtcAsync(
                    cityId: message.CityId,
                    cancellationToken: context.CancellationToken) is not null)
            {
                logger.LogDebug(
                    message: "Skipped household cashflow settlement for deleted cityId={CityId}, correlationId={CorrelationId}.",
                    message.CityId,
                    message.CorrelationId);
                return;
            }

            IReadOnlyList<CityBusiness> businesses = await businessRepository.ListByCityAsync(
                cityId: message.CityId,
                cancellationToken: context.CancellationToken);
            int settledPayrollAccounts = 0;
            int settledConsumerEntries = 0;

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

                if (household.RetailTurnoverAmount <= 0m || businesses.Count == 0)
                    continue;

                IReadOnlyList<CityHouseholdConsumerSpendAllocation> allocations =
                    householdConsumerSpendAllocationPolicy.Allocate(
                        householdId: household.HouseholdId,
                        currentDate: message.CurrentDate,
                        retailTurnover: Money.FromDecimal(household.RetailTurnoverAmount),
                        totalSalesTax: Money.FromDecimal(household.RetailTaxAmount),
                        retailStoreSpend: Money.FromDecimal(household.RetailStoreSpendAmount),
                        serviceSpend: Money.FromDecimal(household.ServiceSpendAmount),
                        municipalSpend: Money.FromDecimal(household.MunicipalSpendAmount),
                        businesses: businesses);

                foreach (CityHouseholdConsumerSpendAllocation allocation in allocations)
                {
                    string referenceCode = BuildReferenceCode(
                        correlationId: message.CorrelationId,
                        householdId: household.HouseholdId,
                        segment: allocation.SegmentKey);
                    bool householdEntryExists = await householdLedgerRepository.ExistsAsync(
                        householdAccountId: account.Id,
                        kind: CityHouseholdAccountLedgerEntryKind.ConsumerPurchase,
                        referenceCode: referenceCode,
                        cancellationToken: context.CancellationToken);
                    bool businessEntryExists = await businessLedgerRepository.ExistsAsync(
                        businessId: allocation.Business.Id,
                        kind: CityBusinessLedgerEntryKind.RetailSale,
                        referenceCode: referenceCode,
                        cancellationToken: context.CancellationToken);

                    if (householdEntryExists || businessEntryExists)
                        continue;

                    allocation.Business.EnsureCompatibleUnit(account.GetUnitProfile());
                    allocation.Business.EnsureCanRecordConsumerSale();

                    account.RecordConsumerPurchase(allocation.GrossAmount);
                    allocation.Business.RecordSettledRetailSale(
                        grossAmount: allocation.GrossAmount,
                        salesTaxAmount: allocation.SalesTaxAmount);

                    await householdLedgerRepository.AddAsync(
                        entry: new CityHouseholdAccountLedgerEntry(
                            id: Guid.NewGuid(),
                            householdAccountId: account.Id,
                            cityId: account.CityId,
                            occurredAtUtc: message.OccurredAtUtc,
                            kind: CityHouseholdAccountLedgerEntryKind.ConsumerPurchase,
                            amount: allocation.GrossAmount,
                            title: allocation.Title,
                            description:
                            $"{allocation.Description} Settled {message.SettledDays} day(s) into the economy ledger.",
                            source: CityHouseholdAccountLedgerEntrySource.Settlement,
                            referenceCode: referenceCode),
                        cancellationToken: context.CancellationToken);
                    await businessLedgerRepository.AddAsync(
                        entry: new CityBusinessLedgerEntry(
                            id: Guid.NewGuid(),
                            businessId: allocation.Business.Id,
                            cityId: allocation.Business.CityId,
                            occurredAtUtc: message.OccurredAtUtc,
                            kind: CityBusinessLedgerEntryKind.RetailSale,
                            amount: allocation.GrossAmount,
                            taxAmount: allocation.SalesTaxAmount,
                            title: allocation.Title,
                            description:
                            $"{allocation.Description} Sales tax was already transferred through aggregate city settlement.",
                            source: CityBusinessLedgerEntrySource.Settlement,
                            referenceCode: referenceCode),
                        cancellationToken: context.CancellationToken);
                    settledConsumerEntries++;
                }
            }

            if (settledPayrollAccounts == 0 && settledConsumerEntries == 0)
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
                "Applied classic city household cashflow settlement for cityId={CityId}, correlationId={CorrelationId}, batch={BatchNumber}/{TotalBatches}, settledPayrollAccounts={SettledPayrollAccounts}, settledConsumerEntries={SettledConsumerEntries}.",
                message.CityId,
                message.CorrelationId,
                message.BatchNumber,
                message.TotalBatches,
                settledPayrollAccounts,
                settledConsumerEntries);
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
