using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityEmployerFinancialStress
{
    public sealed class ApplyCityEmployerFinancialStressCommandHandler(
        ICityPopulationArchiveStateRepository cityPopulationArchiveStateRepository,
        ICityPopulationDeletionStateRepository cityPopulationDeletionStateRepository,
        ICityPopulationEmployerFinancialStressStateRepository employerFinancialStressStateRepository,
        IProcessedIntegrationMessageRepository processedIntegrationMessageRepository,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
        : IRequestHandler<ApplyCityEmployerFinancialStressCommand, ApplyCityEmployerFinancialStressResult>
    {
        private const string WorkplaceExternalReferencePrefix = "classic-city-workplace:";

        public Task<ApplyCityEmployerFinancialStressResult> Handle(
            ApplyCityEmployerFinancialStressCommand request,
            CancellationToken cancellationToken)
        {
            string consumerName = request.ConsumerName;
            var cityId = CityId.From(request.CityId);
            DateTimeOffset occurredAtUtc = request.OccurredAtUtc.ToUniversalTime();

            return unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    bool markedAsProcessed = await processedIntegrationMessageRepository.TryMarkProcessedAsync(
                        consumer: consumerName,
                        messageId: request.IntegrationMessageId,
                        processedAtUtc: timeProvider.GetUtcNow(),
                        cancellationToken: ct);

                    if (!markedAsProcessed)
                        return new ApplyCityEmployerFinancialStressResult(
                            Status: ApplyCityEmployerFinancialStressStatus.Duplicate,
                            AppliedEmployerCount: 0);

                    if (await cityPopulationDeletionStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct) is not null)
                        return new ApplyCityEmployerFinancialStressResult(
                            Status: ApplyCityEmployerFinancialStressStatus.CityDeleted,
                            AppliedEmployerCount: 0);

                    if (await cityPopulationArchiveStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct) is not null)
                        return new ApplyCityEmployerFinancialStressResult(
                            Status: ApplyCityEmployerFinancialStressStatus.CityArchived,
                            AppliedEmployerCount: 0);

                    int appliedEmployerCount = 0;

                    foreach (EmployerFinancialStressSnapshotInput employer in request.Employers)
                    {
                        if (!TryParseWorkplaceId(
                                externalReferenceCode: employer.WorkplaceExternalReferenceCode,
                                workplaceId: out WorkplaceId workplaceId))
                            continue;

                        CityPopulationEmployerFinancialStressState? state =
                            await employerFinancialStressStateRepository.GetByCityAndWorkplaceAsync(
                                cityId: cityId,
                                workplaceId: workplaceId,
                                cancellationToken: ct);

                        if (state is not null && occurredAtUtc < state.LastEvaluatedAtUtc)
                            continue;

                        DateTimeOffset updatedAtUtc = timeProvider.GetUtcNow();

                        if (state is null)
                        {
                            state = CityPopulationEmployerFinancialStressState.Create(
                                cityId: cityId,
                                workplaceId: workplaceId,
                                requestedGrossPayrollAmount: employer.RequestedGrossPayrollAmount,
                                paidGrossPayrollAmount: employer.PaidGrossPayrollAmount,
                                missedGrossPayrollAmount: employer.MissedGrossPayrollAmount,
                                payrollFulfillmentRatio: employer.PayrollFulfillmentRatio,
                                failedPayrollCount: employer.FailedPayrollCount,
                                partialPayrollCount: employer.PartialPayrollCount,
                                currentBalanceAmount: employer.CurrentBalanceAmount,
                                distressScore: employer.DistressScore,
                                hasHiringFreeze: employer.HasHiringFreeze,
                                hasLayoffPressure: employer.HasLayoffPressure,
                                lastEvaluatedAtUtc: occurredAtUtc,
                                updatedAtUtc: updatedAtUtc);

                            await employerFinancialStressStateRepository.AddAsync(
                                state: state,
                                cancellationToken: ct);
                        }
                        else
                            state.ApplySnapshot(
                                requestedGrossPayrollAmount: employer.RequestedGrossPayrollAmount,
                                paidGrossPayrollAmount: employer.PaidGrossPayrollAmount,
                                missedGrossPayrollAmount: employer.MissedGrossPayrollAmount,
                                payrollFulfillmentRatio: employer.PayrollFulfillmentRatio,
                                failedPayrollCount: employer.FailedPayrollCount,
                                partialPayrollCount: employer.PartialPayrollCount,
                                currentBalanceAmount: employer.CurrentBalanceAmount,
                                distressScore: employer.DistressScore,
                                hasHiringFreeze: employer.HasHiringFreeze,
                                hasLayoffPressure: employer.HasLayoffPressure,
                                lastEvaluatedAtUtc: occurredAtUtc,
                                updatedAtUtc: updatedAtUtc);

                        appliedEmployerCount++;
                    }

                    await unitOfWork.SaveChangesAsync(ct);

                    return new ApplyCityEmployerFinancialStressResult(
                        Status: ApplyCityEmployerFinancialStressStatus.Applied,
                        AppliedEmployerCount: appliedEmployerCount);
                },
                cancellationToken: cancellationToken);
        }

        private static bool TryParseWorkplaceId(
            string externalReferenceCode,
            out WorkplaceId workplaceId)
        {
            workplaceId = default(WorkplaceId);

            if (string.IsNullOrWhiteSpace(externalReferenceCode) ||
                !externalReferenceCode.StartsWith(
                    value: WorkplaceExternalReferencePrefix,
                    comparisonType: StringComparison.OrdinalIgnoreCase))
                return false;

            string value = externalReferenceCode[WorkplaceExternalReferencePrefix.Length..];
            if (!Guid.TryParseExact(
                    input: value,
                    format: "N",
                    result: out Guid parsed))
                return false;

            workplaceId = WorkplaceId.From(parsed);
            return true;
        }
    }
}
