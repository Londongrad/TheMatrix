using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers;
using Microsoft.Extensions.Logging;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityWorkplacePayrollSettlementConsumer(
        ICityBusinessRepository businessRepository,
        ICityBusinessLedgerRepository businessLedgerRepository,
        ICityBudgetRepository budgetRepository,
        ICityBudgetLedgerRepository budgetLedgerRepository,
        ICityHouseholdAccountRepository householdAccountRepository,
        ICityHouseholdAccountLedgerRepository householdLedgerRepository,
        ICityPopulationSignalPublisher cityPopulationSignalPublisher,
        ICityEconomyDeletionRepository deletionRepository,
        IEconomyUnitOfWork unitOfWork,
        ILogger<ClassicCityWorkplacePayrollSettlementConsumer> logger)
        : IConsumer<ClassicCityWorkplacePayrollSettlementBatchV1>
    {
        private const int EmployerStressBatchSize = 250;

        public async Task Consume(ConsumeContext<ClassicCityWorkplacePayrollSettlementBatchV1> context)
        {
            ClassicCityWorkplacePayrollSettlementBatchV1 message = context.Message;

            if (await deletionRepository.GetDeletedAtUtcAsync(
                    cityId: message.CityId,
                    cancellationToken: context.CancellationToken) is not null)
            {
                logger.LogDebug(
                    message: "Skipped payroll settlement for deleted cityId={CityId}, correlationId={CorrelationId}.",
                    message.CityId,
                    message.CorrelationId);
                return;
            }

            int recordedPayrollOutcomes = 0;
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
                bool businessShortfallEntryExists = await businessLedgerRepository.ExistsAsync(
                    businessId: business.Id,
                    kind: CityBusinessLedgerEntryKind.PayrollShortfall,
                    referenceCode: referenceCode,
                    cancellationToken: context.CancellationToken);

                if (householdEntryExists || businessEntryExists || businessShortfallEntryExists)
                    continue;

                business.EnsureCompatibleUnit(account.GetUnitProfile());
                business.EnsureCanIssuePayroll();

                var requestedGrossPayroll = Money.FromDecimal(payroll.GrossPayrollAmount);
                var requestedIncomeTax = Money.FromDecimal(payroll.IncomeTaxAmount);
                CityBusinessPayrollSettlementOutcome payrollOutcome = business.SettlePayroll(
                    requestedGrossPayroll: requestedGrossPayroll,
                    requestedIncomeTax: requestedIncomeTax);

                if (!requestedGrossPayroll.IsPositive)
                    continue;

                if (employerPayrollByReference.TryGetValue(
                        key: payroll.WorkplaceExternalReferenceCode,
                        value: out EmployerPayrollSnapshot? payrollSnapshot))
                    employerPayrollByReference[payroll.WorkplaceExternalReferenceCode] = payrollSnapshot with
                    {
                        RequestedGrossPayrollAmount =
                        payrollSnapshot.RequestedGrossPayrollAmount + requestedGrossPayroll.Amount,
                        PaidGrossPayrollAmount =
                        payrollSnapshot.PaidGrossPayrollAmount + payrollOutcome.PaidGrossPayroll.Amount,
                        MissedGrossPayrollAmount =
                        payrollSnapshot.MissedGrossPayrollAmount + payrollOutcome.GrossShortfall.Amount,
                        FailedPayrollCount = payrollSnapshot.FailedPayrollCount +
                                             (payrollOutcome.IsMissed
                                                 ? 1
                                                 : 0),
                        PartialPayrollCount = payrollSnapshot.PartialPayrollCount +
                                              (payrollOutcome.IsPartiallyPaid
                                                  ? 1
                                                  : 0)
                    };
                else
                    employerPayrollByReference[payroll.WorkplaceExternalReferenceCode] = new EmployerPayrollSnapshot(
                        BusinessId: business.Id,
                        WorkplaceExternalReferenceCode: payroll.WorkplaceExternalReferenceCode,
                        Business: business,
                        RequestedGrossPayrollAmount: requestedGrossPayroll.Amount,
                        PaidGrossPayrollAmount: payrollOutcome.PaidGrossPayroll.Amount,
                        MissedGrossPayrollAmount: payrollOutcome.GrossShortfall.Amount,
                        FailedPayrollCount: payrollOutcome.IsMissed
                            ? 1
                            : 0,
                        PartialPayrollCount: payrollOutcome.IsPartiallyPaid
                            ? 1
                            : 0);

                if (payrollOutcome.PaidGrossPayroll.IsPositive)
                {
                    if (payrollOutcome.PaidNetPayroll.IsPositive)
                        account.ReceivePayroll(payrollOutcome.PaidNetPayroll);

                    await businessLedgerRepository.AddAsync(
                        entry: new CityBusinessLedgerEntry(
                            id: Guid.NewGuid(),
                            businessId: business.Id,
                            cityId: business.CityId,
                            occurredAtUtc: message.OccurredAtUtc,
                            kind: CityBusinessLedgerEntryKind.PayrollExpense,
                            amount: payrollOutcome.PaidGrossPayroll,
                            taxAmount: payrollOutcome.PaidIncomeTax,
                            title: "Settled workplace payroll",
                            description:
                            $"Payroll settled from classic city workplace '{payroll.JobTitle}' for {message.SettledDays} day(s).",
                            source: CityBusinessLedgerEntrySource.Settlement,
                            referenceCode: referenceCode),
                        cancellationToken: context.CancellationToken);

                    if (payrollOutcome.PaidNetPayroll.IsPositive)
                        await householdLedgerRepository.AddAsync(
                            entry: new CityHouseholdAccountLedgerEntry(
                                id: Guid.NewGuid(),
                                householdAccountId: account.Id,
                                cityId: account.CityId,
                                occurredAtUtc: message.OccurredAtUtc,
                                kind: CityHouseholdAccountLedgerEntryKind.PayrollIncome,
                                amount: payrollOutcome.PaidNetPayroll,
                                title: "Settled workplace payroll",
                                description:
                                $"Take-home payroll settled from classic city workplace '{payroll.JobTitle}' for {message.SettledDays} day(s).",
                                source: CityHouseholdAccountLedgerEntrySource.Settlement,
                                referenceCode: referenceCode),
                            cancellationToken: context.CancellationToken);

                    if (payrollOutcome.PaidIncomeTax.IsPositive)
                    {
                        CityBudget budget = await CityBudgetInitializationSupport.EnsureExistsAsync(
                            cityId: business.CityId,
                            budgetRepository: budgetRepository,
                            unitOfWork: unitOfWork,
                            cancellationToken: context.CancellationToken);
                        budget.EnsureCompatibleUnit(business.GetUnitProfile());

                        var budgetEntry = new CityBudgetLedgerEntry(
                            id: Guid.NewGuid(),
                            cityId: business.CityId,
                            occurredAtUtc: message.OccurredAtUtc,
                            kind: CityBudgetLedgerEntryKind.Revenue,
                            category: CityBudgetCategory.Taxation,
                            amount: payrollOutcome.PaidIncomeTax,
                            title: "Settled workplace payroll withholding",
                            description:
                            $"Income tax withheld from classic city workplace '{payroll.JobTitle}' payroll.",
                            source: CityBudgetLedgerEntrySource.PayrollWithholding,
                            referenceCode: referenceCode);

                        budget.ApplyLedgerEntry(budgetEntry);
                        await budgetLedgerRepository.AddAsync(
                            entry: budgetEntry,
                            cancellationToken: context.CancellationToken);
                    }

                    recordedPayrollOutcomes++;
                }

                if (payrollOutcome.GrossShortfall.IsPositive)
                {
                    await businessLedgerRepository.AddAsync(
                        entry: new CityBusinessLedgerEntry(
                            id: Guid.NewGuid(),
                            businessId: business.Id,
                            cityId: business.CityId,
                            occurredAtUtc: message.OccurredAtUtc,
                            kind: CityBusinessLedgerEntryKind.PayrollShortfall,
                            amount: payrollOutcome.GrossShortfall,
                            taxAmount: Money.Zero,
                            title: "Payroll shortfall",
                            description:
                            $"Classic city workplace '{payroll.JobTitle}' could not cover the full requested payroll for {message.SettledDays} day(s).",
                            source: CityBusinessLedgerEntrySource.Settlement,
                            referenceCode: referenceCode),
                        cancellationToken: context.CancellationToken);
                    recordedPayrollOutcomes++;
                }
            }

            if (recordedPayrollOutcomes == 0)
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
                "Applied classic city workplace payroll settlement for cityId={CityId}, correlationId={CorrelationId}, batch={BatchNumber}/{TotalBatches}, recordedPayrollOutcomes={RecordedPayrollOutcomes}.",
                message.CityId,
                message.CorrelationId,
                message.BatchNumber,
                message.TotalBatches,
                recordedPayrollOutcomes);
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
                        requestedGrossPayrollAmount: snapshot.RequestedGrossPayrollAmount,
                        paidGrossPayrollAmount: snapshot.PaidGrossPayrollAmount,
                        missedGrossPayrollAmount: snapshot.MissedGrossPayrollAmount,
                        failedPayrollCount: snapshot.FailedPayrollCount,
                        partialPayrollCount: snapshot.PartialPayrollCount);
                    bool hasHiringFreeze = distressScore >= 0.55m;
                    bool hasLayoffPressure = distressScore >= 0.72m;
                    decimal payrollFulfillmentRatio = snapshot.RequestedGrossPayrollAmount <= 0m
                        ? 1m
                        : Math.Clamp(
                            value: snapshot.PaidGrossPayrollAmount / snapshot.RequestedGrossPayrollAmount,
                            min: 0m,
                            max: 1m);

                    return new ClassicCityEmployerFinancialStressItemV1(
                        EmployerBusinessId: snapshot.BusinessId,
                        WorkplaceExternalReferenceCode: snapshot.WorkplaceExternalReferenceCode,
                        RequestedGrossPayrollAmount: decimal.Round(
                            d: snapshot.RequestedGrossPayrollAmount,
                            decimals: 2,
                            mode: MidpointRounding.AwayFromZero),
                        PaidGrossPayrollAmount: decimal.Round(
                            d: snapshot.PaidGrossPayrollAmount,
                            decimals: 2,
                            mode: MidpointRounding.AwayFromZero),
                        MissedGrossPayrollAmount: decimal.Round(
                            d: snapshot.MissedGrossPayrollAmount,
                            decimals: 2,
                            mode: MidpointRounding.AwayFromZero),
                        PayrollFulfillmentRatio: decimal.Round(
                            d: payrollFulfillmentRatio,
                            decimals: 4,
                            mode: MidpointRounding.AwayFromZero),
                        FailedPayrollCount: snapshot.FailedPayrollCount,
                        PartialPayrollCount: snapshot.PartialPayrollCount,
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
               .OrderBy(
                    keySelector: x => x.WorkplaceExternalReferenceCode,
                    comparer: StringComparer.Ordinal)
               .ToArray();

            if (items.Length == 0)
                return [];

            ClassicCityEmployerFinancialStressBatchV1[] batches = items
               .Chunk(EmployerStressBatchSize)
               .Select((
                    chunk,
                    index) => new ClassicCityEmployerFinancialStressBatchV1(
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
            decimal requestedGrossPayrollAmount,
            decimal paidGrossPayrollAmount,
            decimal missedGrossPayrollAmount,
            int failedPayrollCount,
            int partialPayrollCount)
        {
            if (requestedGrossPayrollAmount <= 0m)
                return currentBalanceAmount < 0m
                    ? 0.60m
                    : 0m;

            decimal distressScore = 0m;
            decimal nonNegativeBalance = Math.Max(
                val1: 0m,
                val2: currentBalanceAmount);
            decimal payrollShortfallRatio = Math.Clamp(
                value: missedGrossPayrollAmount / requestedGrossPayrollAmount,
                min: 0m,
                max: 1m);
            decimal paidPayrollRatio = Math.Clamp(
                value: paidGrossPayrollAmount / requestedGrossPayrollAmount,
                min: 0m,
                max: 1m);

            distressScore += payrollShortfallRatio * 0.55m;

            if (paidPayrollRatio < 1m && partialPayrollCount > 0)
                distressScore += 0.10m;

            if (failedPayrollCount > 0)
                distressScore += Math.Min(
                    val1: 0.20m,
                    val2: failedPayrollCount * 0.08m);

            if (currentBalanceAmount <= requestedGrossPayrollAmount * 0.25m)
                distressScore += 0.20m;

            if (currentBalanceAmount <= requestedGrossPayrollAmount * 0.10m)
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
            decimal RequestedGrossPayrollAmount,
            decimal PaidGrossPayrollAmount,
            decimal MissedGrossPayrollAmount,
            int FailedPayrollCount,
            int PartialPayrollCount);
    }
}
