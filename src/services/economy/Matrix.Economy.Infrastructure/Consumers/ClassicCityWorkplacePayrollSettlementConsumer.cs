using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
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
        ICityPopulationSignalPublisher cityPopulationSignalPublisher,
        IEconomyUnitOfWork unitOfWork,
        ILogger<ClassicCityWorkplacePayrollSettlementConsumer> logger)
        : IConsumer<ClassicCityWorkplacePayrollSettlementBatchV1>
    {
        private const int EmployerStressBatchSize = 250;

        public async Task Consume(ConsumeContext<ClassicCityWorkplacePayrollSettlementBatchV1> context)
        {
            ClassicCityWorkplacePayrollSettlementBatchV1 message = context.Message;
            int settledPayrollEntries = 0;
            var employerPayrollByReference = new Dictionary<string, EmployerPayrollSnapshot>(
                comparer: StringComparer.Ordinal);

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

                if (employerPayrollByReference.TryGetValue(
                        key: payroll.WorkplaceExternalReferenceCode,
                        value: out EmployerPayrollSnapshot? payrollSnapshot))
                    employerPayrollByReference[payroll.WorkplaceExternalReferenceCode] = payrollSnapshot with
                    {
                        GrossPayrollAmount = payrollSnapshot.GrossPayrollAmount + grossPayroll.Amount
                    };
                else
                    employerPayrollByReference[payroll.WorkplaceExternalReferenceCode] = new EmployerPayrollSnapshot(
                        BusinessId: business.Id,
                        WorkplaceExternalReferenceCode: payroll.WorkplaceExternalReferenceCode,
                        Business: business,
                        GrossPayrollAmount: grossPayroll.Amount);

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

            foreach (ClassicCityEmployerFinancialStressBatchV1 batch in BuildEmployerStressBatches(
                         message: message,
                         employerSnapshots: employerPayrollByReference.Values))
                await cityPopulationSignalPublisher.PublishClassicCityEmployerFinancialStressBatchAsync(
                    batch: batch,
                    cancellationToken: context.CancellationToken);

            logger.LogInformation(
                message:
                "Applied classic city workplace payroll settlement for cityId={CityId}, correlationId={CorrelationId}, batch={BatchNumber}/{TotalBatches}, settledPayrollEntries={SettledPayrollEntries}.",
                message.CityId,
                message.CorrelationId,
                message.BatchNumber,
                message.TotalBatches,
                settledPayrollEntries);
        }

        private static ClassicCityEmployerFinancialStressBatchV1[] BuildEmployerStressBatches(
            ClassicCityWorkplacePayrollSettlementBatchV1 message,
            IEnumerable<EmployerPayrollSnapshot> employerSnapshots)
        {
            ClassicCityEmployerFinancialStressItemV1[] items = employerSnapshots
               .Select(snapshot =>
                {
                    decimal distressScore = CalculateDistressScore(
                        currentBalanceAmount: snapshot.Business.Balance.Amount,
                        recentGrossPayrollAmount: snapshot.GrossPayrollAmount);
                    bool hasHiringFreeze = distressScore >= 0.55m;
                    bool hasLayoffPressure = distressScore >= 0.72m;

                    return new ClassicCityEmployerFinancialStressItemV1(
                        EmployerBusinessId: snapshot.BusinessId,
                        WorkplaceExternalReferenceCode: snapshot.WorkplaceExternalReferenceCode,
                        RecentGrossPayrollAmount: decimal.Round(
                            d: snapshot.GrossPayrollAmount,
                            decimals: 2,
                            mode: MidpointRounding.AwayFromZero),
                        CurrentBalanceAmount: decimal.Round(
                            d: snapshot.Business.Balance.Amount,
                            decimals: 2,
                            mode: MidpointRounding.AwayFromZero),
                        DistressScore: decimal.Round(
                            d: distressScore,
                            decimals: 4,
                            mode: MidpointRounding.AwayFromZero),
                        HasHiringFreeze: hasHiringFreeze,
                        HasLayoffPressure: hasLayoffPressure);
                })
               .OrderBy(x => x.WorkplaceExternalReferenceCode, StringComparer.Ordinal)
               .ToArray();

            if (items.Length == 0)
                return [];

            ClassicCityEmployerFinancialStressBatchV1[] batches = items
               .Chunk(EmployerStressBatchSize)
               .Select((chunk, index) => new ClassicCityEmployerFinancialStressBatchV1(
                    CityId: message.CityId,
                    BatchNumber: index + 1,
                    TotalBatches: 0,
                    Employers: chunk,
                    CorrelationId: $"{message.CorrelationId}:employer-stress",
                    OccurredAtUtc: message.OccurredAtUtc))
               .ToArray();

            for (int i = 0; i < batches.Length; i++)
                batches[i] = batches[i] with
                {
                    TotalBatches = batches.Length
                };

            return batches;
        }

        private static decimal CalculateDistressScore(
            decimal currentBalanceAmount,
            decimal recentGrossPayrollAmount)
        {
            if (recentGrossPayrollAmount <= 0m)
                return currentBalanceAmount < 0m
                    ? 0.60m
                    : 0m;

            decimal distressScore = 0m;
            decimal nonNegativeBalance = Math.Max(
                val1: 0m,
                val2: currentBalanceAmount);
            decimal uncoveredPayrollRatio = Math.Clamp(
                value: (recentGrossPayrollAmount - nonNegativeBalance) / recentGrossPayrollAmount,
                min: 0m,
                max: 1m);

            distressScore += uncoveredPayrollRatio * 0.45m;

            if (currentBalanceAmount <= recentGrossPayrollAmount * 0.25m)
                distressScore += 0.20m;

            if (currentBalanceAmount <= recentGrossPayrollAmount * 0.10m)
                distressScore += 0.10m;

            if (currentBalanceAmount < 0m)
                distressScore += 0.25m;

            return Math.Clamp(
                value: distressScore,
                min: 0m,
                max: 1m);
        }

        private static string BuildReferenceCode(
            string correlationId,
            Guid householdId,
            Guid workplaceId)
        {
            return $"{correlationId}:household:{householdId:N}:workplace:{workplaceId:N}:payroll";
        }

        private sealed record EmployerPayrollSnapshot(
            Guid BusinessId,
            string WorkplaceExternalReferenceCode,
            CityBusiness Business,
            decimal GrossPayrollAmount);
    }
}
