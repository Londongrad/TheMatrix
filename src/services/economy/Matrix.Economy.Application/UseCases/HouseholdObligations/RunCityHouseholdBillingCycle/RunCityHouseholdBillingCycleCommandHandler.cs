using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.HouseholdObligations.Common;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using MediatR;

namespace Matrix.Economy.Application.UseCases.HouseholdObligations.RunCityHouseholdBillingCycle
{
    public sealed class RunCityHouseholdBillingCycleCommandHandler(
        ICityHouseholdObligationRepository obligationRepository,
        ICityHouseholdAccountRepository householdAccountRepository,
        ICityPopulationSignalPublisher cityPopulationSignalPublisher,
        HouseholdObligationChargeSupport chargeSupport,
        IEconomyUnitOfWork unitOfWork)
        : IRequestHandler<RunCityHouseholdBillingCycleCommand, RunCityHouseholdBillingCycleResultDto>
    {
        private const int FinancialStressBatchSize = 500;

        public async Task<RunCityHouseholdBillingCycleResultDto> Handle(
            RunCityHouseholdBillingCycleCommand request,
            CancellationToken cancellationToken)
        {
            DateTimeOffset asOfUtc = request.AsOfUtc ?? DateTimeOffset.UtcNow;

            IReadOnlyList<CityHouseholdObligation> dueObligations = await obligationRepository.ListDueByCityAsync(
                request.CityId,
                asOfUtc,
                cancellationToken);

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

            await unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (ClassicCityHouseholdFinancialStressBatchV1 batch in await BuildFinancialStressBatchesAsync(
                         cityId: request.CityId,
                         asOfUtc: asOfUtc,
                         dueObligations: dueObligations,
                         householdAccountRepository: householdAccountRepository,
                         cancellationToken: cancellationToken))
            {
                await cityPopulationSignalPublisher.PublishClassicCityHouseholdFinancialStressBatchAsync(
                    batch: batch,
                    cancellationToken: cancellationToken);
            }

            return new RunCityHouseholdBillingCycleResultDto(
                CityId: request.CityId,
                AsOfUtc: asOfUtc.ToString("O"),
                ChargedObligations: dueObligations.Count,
                TotalChargedAmount: totalChargedAmount,
                TotalTaxAmount: totalTaxAmount);
        }

        private static async Task<ClassicCityHouseholdFinancialStressBatchV1[]> BuildFinancialStressBatchesAsync(
            Guid cityId,
            DateTimeOffset asOfUtc,
            IReadOnlyList<CityHouseholdObligation> dueObligations,
            ICityHouseholdAccountRepository householdAccountRepository,
            CancellationToken cancellationToken)
        {
            CityHouseholdObligation[] overdueObligations = dueObligations
                .Where(x => x.IsActive && x.NextChargeDueAtUtc <= asOfUtc)
                .ToArray();

            if (overdueObligations.Length == 0)
                return [];

            var items = new List<ClassicCityHouseholdFinancialStressItemV1>();

            foreach (IGrouping<Guid, CityHouseholdObligation> group in overdueObligations.GroupBy(x => x.HouseholdAccountId))
            {
                CityHouseholdAccount? account = await householdAccountRepository.GetByIdAsync(
                    householdAccountId: group.Key,
                    cancellationToken: cancellationToken);

                if (account is null || string.IsNullOrWhiteSpace(account.ExternalReferenceCode))
                    continue;

                CityHouseholdObligation[] obligations = group.ToArray();
                decimal overdueAmount = obligations.Sum(x => x.ChargeAmount.Amount);
                int overdueRentCount = obligations.Count(x => x.Kind == CityHouseholdObligationKind.Rent);
                int overdueUtilityCount = obligations.Count(x => x.Kind == CityHouseholdObligationKind.Utilities);
                decimal distressScore = Math.Clamp(
                    (obligations.Length * 0.18m)
                    + (overdueRentCount * 0.22m)
                    + (overdueUtilityCount * 0.12m)
                    + Math.Min(0.40m, overdueAmount / 1000m),
                    0m,
                    1m);

                items.Add(
                    new ClassicCityHouseholdFinancialStressItemV1(
                        HouseholdAccountId: account.Id,
                        HouseholdExternalReferenceCode: account.ExternalReferenceCode,
                        OverdueObligationCount: obligations.Length,
                        OverdueRentCount: overdueRentCount,
                        OverdueUtilityCount: overdueUtilityCount,
                        TotalOverdueAmount: overdueAmount,
                        DistressScore: decimal.Round(distressScore, 4, MidpointRounding.AwayFromZero)));
            }

            if (items.Count == 0)
                return [];

            string correlationId = $"classic-city:{cityId:N}:household-financial-stress:{Guid.NewGuid():N}";
            ClassicCityHouseholdFinancialStressBatchV1[] batches = items
                .Chunk(FinancialStressBatchSize)
                .Select((chunk, index) => new ClassicCityHouseholdFinancialStressBatchV1(
                    CityId: cityId,
                    BatchNumber: index + 1,
                    TotalBatches: 0,
                    Households: chunk,
                    CorrelationId: correlationId,
                    OccurredAtUtc: asOfUtc))
                .ToArray();

            for (int i = 0; i < batches.Length; i++)
            {
                batches[i] = batches[i] with { TotalBatches = batches.Length };
            }

            return batches;
        }
    }
}
