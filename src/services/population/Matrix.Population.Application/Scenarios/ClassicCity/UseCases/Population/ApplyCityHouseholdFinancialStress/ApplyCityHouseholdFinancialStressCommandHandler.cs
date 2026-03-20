using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityHouseholdFinancialStress
{
    public sealed class ApplyCityHouseholdFinancialStressCommandHandler(
        ICityPopulationArchiveStateRepository cityPopulationArchiveStateRepository,
        ICityPopulationDeletionStateRepository cityPopulationDeletionStateRepository,
        ICityPopulationHouseholdFinancialStressStateRepository householdFinancialStressStateRepository,
        IProcessedIntegrationMessageRepository processedIntegrationMessageRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<ApplyCityHouseholdFinancialStressCommand, ApplyCityHouseholdFinancialStressResult>
    {
        private const string HouseholdExternalReferencePrefix = "classic-city-household:";

        public Task<ApplyCityHouseholdFinancialStressResult> Handle(
            ApplyCityHouseholdFinancialStressCommand request,
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
                        processedAtUtc: DateTimeOffset.UtcNow,
                        cancellationToken: ct);

                    if (!markedAsProcessed)
                        return new ApplyCityHouseholdFinancialStressResult(
                            Status: ApplyCityHouseholdFinancialStressStatus.Duplicate,
                            AppliedHouseholdCount: 0);

                    if (await cityPopulationDeletionStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct) is not null)
                        return new ApplyCityHouseholdFinancialStressResult(
                            Status: ApplyCityHouseholdFinancialStressStatus.CityDeleted,
                            AppliedHouseholdCount: 0);

                    if (await cityPopulationArchiveStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct) is not null)
                        return new ApplyCityHouseholdFinancialStressResult(
                            Status: ApplyCityHouseholdFinancialStressStatus.CityArchived,
                            AppliedHouseholdCount: 0);

                    int appliedHouseholdCount = 0;

                    foreach (HouseholdFinancialStressSnapshotInput household in request.Households)
                    {
                        if (!TryParseHouseholdId(
                                externalReferenceCode: household.HouseholdExternalReferenceCode,
                                householdId: out HouseholdId householdId))
                            continue;

                        CityPopulationHouseholdFinancialStressState? state =
                            await householdFinancialStressStateRepository.GetByCityAndHouseholdAsync(
                                cityId: cityId,
                                householdId: householdId,
                                cancellationToken: ct);

                        if (state is not null && occurredAtUtc < state.LastEvaluatedAtUtc)
                            continue;

                        DateTimeOffset updatedAtUtc = DateTimeOffset.UtcNow;

                        if (state is null)
                        {
                            state = CityPopulationHouseholdFinancialStressState.Create(
                                cityId: cityId,
                                householdId: householdId,
                                overdueObligationCount: household.OverdueObligationCount,
                                overdueRentCount: household.OverdueRentCount,
                                overdueUtilityCount: household.OverdueUtilityCount,
                                arrearsObligationCount: household.ArrearsObligationCount,
                                serviceCutoffCount: household.ServiceCutoffCount,
                                evictionNoticeCount: household.EvictionNoticeCount,
                                evictionEligibleCount: household.EvictionEligibleCount,
                                oldestOverdueAgeDays: household.OldestOverdueAgeDays,
                                totalOverdueAmount: household.TotalOverdueAmount,
                                distressScore: household.DistressScore,
                                lastEvaluatedAtUtc: occurredAtUtc,
                                updatedAtUtc: updatedAtUtc);

                            await householdFinancialStressStateRepository.AddAsync(
                                state: state,
                                cancellationToken: ct);
                        }
                        else
                            state.ApplySnapshot(
                                overdueObligationCount: household.OverdueObligationCount,
                                overdueRentCount: household.OverdueRentCount,
                                overdueUtilityCount: household.OverdueUtilityCount,
                                arrearsObligationCount: household.ArrearsObligationCount,
                                serviceCutoffCount: household.ServiceCutoffCount,
                                evictionNoticeCount: household.EvictionNoticeCount,
                                evictionEligibleCount: household.EvictionEligibleCount,
                                oldestOverdueAgeDays: household.OldestOverdueAgeDays,
                                totalOverdueAmount: household.TotalOverdueAmount,
                                distressScore: household.DistressScore,
                                lastEvaluatedAtUtc: occurredAtUtc,
                                updatedAtUtc: updatedAtUtc);

                        appliedHouseholdCount++;
                    }

                    await unitOfWork.SaveChangesAsync(ct);

                    return new ApplyCityHouseholdFinancialStressResult(
                        Status: ApplyCityHouseholdFinancialStressStatus.Applied,
                        AppliedHouseholdCount: appliedHouseholdCount);
                },
                cancellationToken: cancellationToken);
        }

        private static bool TryParseHouseholdId(
            string externalReferenceCode,
            out HouseholdId householdId)
        {
            householdId = default(HouseholdId);

            if (string.IsNullOrWhiteSpace(externalReferenceCode) ||
                !externalReferenceCode.StartsWith(
                    value: HouseholdExternalReferencePrefix,
                    comparisonType: StringComparison.OrdinalIgnoreCase))
                return false;

            string value = externalReferenceCode[HouseholdExternalReferencePrefix.Length..];
            if (!Guid.TryParseExact(
                    input: value,
                    format: "N",
                    result: out Guid parsed))
                return false;

            householdId = HouseholdId.From(parsed);
            return true;
        }
    }
}
