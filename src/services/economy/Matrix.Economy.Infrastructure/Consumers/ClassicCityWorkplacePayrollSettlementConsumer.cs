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
    public sealed class ClassicCityWorkplacePayrollSettlementConsumer(
        ICityBusinessRepository businessRepository,
        ICityBusinessLedgerRepository businessLedgerRepository,
        ICityHouseholdAccountRepository householdAccountRepository,
        ICityHouseholdAccountLedgerRepository householdLedgerRepository,
        IEconomyUnitOfWork unitOfWork,
        ILogger<ClassicCityWorkplacePayrollSettlementConsumer> logger)
        : IConsumer<ClassicCityWorkplacePayrollSettlementBatchV1>
    {
        public async Task Consume(ConsumeContext<ClassicCityWorkplacePayrollSettlementBatchV1> context)
        {
            ClassicCityWorkplacePayrollSettlementBatchV1 message = context.Message;
            int settledPayrollEntries = 0;

            foreach (ClassicCityWorkplacePayrollSettlementItemV1 payroll in message.Payrolls)
            {
                CityHouseholdAccount? account = await householdAccountRepository.GetByCityAndExternalReferenceCodeAsync(
                    cityId: message.CityId,
                    externalReferenceCode: payroll.HouseholdExternalReferenceCode,
                    cancellationToken: context.CancellationToken);

                if (account is null)
                {
                    logger.LogWarning(
                        message:
                        "Skipped workplace payroll settlement for cityId={CityId}, correlationId={CorrelationId}, householdRef={HouseholdRef}; account was not found.",
                        message.CityId,
                        message.CorrelationId,
                        payroll.HouseholdExternalReferenceCode);
                    continue;
                }

                CityBusiness? business = await businessRepository.GetByCityAndExternalReferenceCodeAsync(
                    cityId: message.CityId,
                    externalReferenceCode: payroll.WorkplaceExternalReferenceCode,
                    cancellationToken: context.CancellationToken);

                if (business is null)
                {
                    logger.LogWarning(
                        message:
                        "Skipped workplace payroll settlement for cityId={CityId}, correlationId={CorrelationId}, workplaceRef={WorkplaceRef}; employer business was not found.",
                        message.CityId,
                        message.CorrelationId,
                        payroll.WorkplaceExternalReferenceCode);
                    continue;
                }

                string referenceCode = BuildReferenceCode(
                    correlationId: message.CorrelationId,
                    householdId: payroll.HouseholdId,
                    workplaceId: payroll.WorkplaceId);

                bool householdEntryExists = await householdLedgerRepository.ExistsAsync(
                    householdAccountId: account.Id,
                    kind: CityHouseholdAccountLedgerEntryKind.PayrollIncome,
                    referenceCode: referenceCode,
                    cancellationToken: context.CancellationToken);
                bool businessEntryExists = await businessLedgerRepository.ExistsAsync(
                    businessId: business.Id,
                    kind: CityBusinessLedgerEntryKind.PayrollExpense,
                    referenceCode: referenceCode,
                    cancellationToken: context.CancellationToken);

                if (householdEntryExists || businessEntryExists)
                    continue;

                business.EnsureCompatibleUnit(account.GetUnitProfile());
                business.EnsureCanIssuePayroll();

                var grossPayroll = Money.FromDecimal(payroll.GrossPayrollAmount);
                var incomeTax = Money.FromDecimal(payroll.IncomeTaxAmount);
                var netPayroll = Money.FromDecimal(payroll.NetPayrollAmount);

                if (!grossPayroll.IsPositive || !netPayroll.IsPositive)
                    continue;

                business.RecordOperatingExpense(grossPayroll);
                account.ReceivePayroll(netPayroll);

                await businessLedgerRepository.AddAsync(
                    entry: new CityBusinessLedgerEntry(
                        id: Guid.NewGuid(),
                        businessId: business.Id,
                        cityId: business.CityId,
                        occurredAtUtc: message.OccurredAtUtc,
                        kind: CityBusinessLedgerEntryKind.PayrollExpense,
                        amount: grossPayroll,
                        taxAmount: incomeTax,
                        title: "Settled workplace payroll",
                        description:
                        $"Payroll settled from classic city workplace '{payroll.JobTitle}' for {message.SettledDays} day(s). Income tax was already transferred through aggregate city settlement.",
                        source: CityBusinessLedgerEntrySource.Settlement,
                        referenceCode: referenceCode),
                    cancellationToken: context.CancellationToken);

                await householdLedgerRepository.AddAsync(
                    entry: new CityHouseholdAccountLedgerEntry(
                        id: Guid.NewGuid(),
                        householdAccountId: account.Id,
                        cityId: account.CityId,
                        occurredAtUtc: message.OccurredAtUtc,
                        kind: CityHouseholdAccountLedgerEntryKind.PayrollIncome,
                        amount: netPayroll,
                        title: "Settled workplace payroll",
                        description:
                        $"Take-home payroll settled from classic city workplace '{payroll.JobTitle}' for {message.SettledDays} day(s).",
                        source: CityHouseholdAccountLedgerEntrySource.Settlement,
                        referenceCode: referenceCode),
                    cancellationToken: context.CancellationToken);

                settledPayrollEntries++;
            }

            if (settledPayrollEntries == 0)
            {
                logger.LogDebug(
                    message:
                    "Skipped classic city workplace payroll settlement for cityId={CityId}, correlationId={CorrelationId}, batch={BatchNumber}/{TotalBatches}.",
                    message.CityId,
                    message.CorrelationId,
                    message.BatchNumber,
                    message.TotalBatches);
                return;
            }

            await unitOfWork.SaveChangesAsync(context.CancellationToken);

            logger.LogInformation(
                message:
                "Applied classic city workplace payroll settlement for cityId={CityId}, correlationId={CorrelationId}, batch={BatchNumber}/{TotalBatches}, settledPayrollEntries={SettledPayrollEntries}.",
                message.CityId,
                message.CorrelationId,
                message.BatchNumber,
                message.TotalBatches,
                settledPayrollEntries);
        }

        private static string BuildReferenceCode(
            string correlationId,
            Guid householdId,
            Guid workplaceId)
        {
            return $"{correlationId}:household:{householdId:N}:workplace:{workplaceId:N}:payroll";
        }
    }
}
