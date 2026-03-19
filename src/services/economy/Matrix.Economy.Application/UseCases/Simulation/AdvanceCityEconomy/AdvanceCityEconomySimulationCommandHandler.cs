using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.Simulation.Common;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Simulation.AdvanceCityEconomy
{
    public sealed class AdvanceCityEconomySimulationCommandHandler(
        ICityEconomyProgressionStateRepository progressionStateRepository,
        ICityPopulationSignalPublisher cityPopulationSignalPublisher,
        CityEconomyRecurringCycleExecutionService recurringCycleExecutionService,
        IEconomyUnitOfWork unitOfWork)
        : IRequestHandler<AdvanceCityEconomySimulationCommand, AdvanceCityEconomySimulationResult>
    {
        public async Task<AdvanceCityEconomySimulationResult> Handle(
            AdvanceCityEconomySimulationCommand request,
            CancellationToken cancellationToken)
        {
            DateOnly fromDate = DateOnly.FromDateTime(request.FromSimTimeUtc.UtcDateTime);
            DateOnly toDate = DateOnly.FromDateTime(request.ToSimTimeUtc.UtcDateTime);
            CityEconomyProgressionState? state = await progressionStateRepository.GetByCityAsync(
                cityId: request.CityId,
                cancellationToken: cancellationToken);

            if (state is not null)
            {
                if (toDate < state.LastProcessedDate || request.TickId < state.LastCompletedTickId)
                    return CreateResult(AdvanceCityEconomySimulationStatus.OutOfOrder);

                if (request.TickId == state.LastCompletedTickId && toDate <= state.LastProcessedDate)
                    return CreateResult(AdvanceCityEconomySimulationStatus.Duplicate);
            }

            DateOnly previousDate = state?.LastProcessedDate ?? fromDate;
            if (state is null)
            {
                state = CityEconomyProgressionState.Create(
                    cityId: request.CityId,
                    lastCompletedTickId: 0,
                    lastProcessedDate: previousDate,
                    updatedAtUtc: DateTimeOffset.UtcNow);
                await progressionStateRepository.AddAsync(
                    state: state,
                    cancellationToken: cancellationToken);
            }

            int processedDays = 0;
            int chargedObligations = 0;
            int remittedBusinesses = 0;
            int municipalProviderPayments = 0;
            decimal totalChargedAmount = 0m;
            decimal totalTaxRemittedAmount = 0m;
            decimal totalMunicipalDisbursedAmount = 0m;

            foreach (DateOnly cycleDate in EnumerateUnprocessedDates(
                         previousDate: previousDate,
                         currentDate: toDate))
            {
                DateTimeOffset cycleAsOfUtc = ResolveCycleAsOfUtc(
                    cycleDate: cycleDate,
                    finalDate: toDate,
                    finalSimTimeUtc: request.ToSimTimeUtc);

                CityEconomyBillingCycleExecutionResult billingResult =
                    await recurringCycleExecutionService.ExecuteBillingAsync(
                        cityId: request.CityId,
                        asOfUtc: cycleAsOfUtc,
                        cancellationToken: cancellationToken);
                var taxResult = await recurringCycleExecutionService.ExecuteTaxCycleAsync(
                    cityId: request.CityId,
                    budgetCategory: CityBudgetCategory.Taxation,
                    cancellationToken: cancellationToken);
                var municipalResult = await recurringCycleExecutionService.ExecuteMunicipalOperatingCycleAsync(
                    cityId: request.CityId,
                    cancellationToken: cancellationToken);

                chargedObligations += billingResult.Result.ChargedObligations;
                remittedBusinesses += taxResult.RemittedBusinesses;
                municipalProviderPayments += municipalResult.ProviderPayments;
                totalChargedAmount += billingResult.Result.TotalChargedAmount;
                totalTaxRemittedAmount += taxResult.TotalRemittedAmount;
                totalMunicipalDisbursedAmount += municipalResult.TotalDisbursedAmount;

                state.AdvanceProcessedDate(
                    processedDate: cycleDate,
                    updatedAtUtc: DateTimeOffset.UtcNow);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                foreach (var batch in billingResult.FinancialStressBatches)
                    await cityPopulationSignalPublisher.PublishClassicCityHouseholdFinancialStressBatchAsync(
                        batch: batch,
                        cancellationToken: cancellationToken);

                processedDays++;
            }

            state.AdvanceProcessedDate(
                processedDate: toDate,
                updatedAtUtc: DateTimeOffset.UtcNow);
            state.MarkTickCompleted(
                tickId: request.TickId,
                updatedAtUtc: DateTimeOffset.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new AdvanceCityEconomySimulationResult(
                Status: AdvanceCityEconomySimulationStatus.Applied,
                ProcessedDays: processedDays,
                ChargedObligations: chargedObligations,
                RemittedBusinesses: remittedBusinesses,
                MunicipalProviderPayments: municipalProviderPayments,
                TotalChargedAmount: totalChargedAmount,
                TotalTaxRemittedAmount: totalTaxRemittedAmount,
                TotalMunicipalDisbursedAmount: totalMunicipalDisbursedAmount);
        }

        private static IEnumerable<DateOnly> EnumerateUnprocessedDates(
            DateOnly previousDate,
            DateOnly currentDate)
        {
            for (int dayNumber = previousDate.DayNumber + 1; dayNumber <= currentDate.DayNumber; dayNumber++)
                yield return DateOnly.FromDayNumber(dayNumber);
        }

        private static DateTimeOffset ResolveCycleAsOfUtc(
            DateOnly cycleDate,
            DateOnly finalDate,
            DateTimeOffset finalSimTimeUtc)
        {
            if (cycleDate == finalDate)
                return finalSimTimeUtc;

            DateTime endOfDayUtc = DateTime.SpecifyKind(
                value: cycleDate.ToDateTime(TimeOnly.MaxValue),
                kind: DateTimeKind.Utc);

            return new DateTimeOffset(endOfDayUtc);
        }

        private static AdvanceCityEconomySimulationResult CreateResult(
            AdvanceCityEconomySimulationStatus status)
        {
            return new AdvanceCityEconomySimulationResult(
                Status: status,
                ProcessedDays: 0,
                ChargedObligations: 0,
                RemittedBusinesses: 0,
                MunicipalProviderPayments: 0,
                TotalChargedAmount: 0m,
                TotalTaxRemittedAmount: 0m,
                TotalMunicipalDisbursedAmount: 0m);
        }
    }
}
