using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Scenarios.ClassicCity.Population;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityHouseholdFinancialStress;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityHouseholdFinancialStressConsumer(
        IMediator mediator,
        ILogger<ClassicCityHouseholdFinancialStressConsumer> logger)
        : IConsumer<ClassicCityHouseholdFinancialStressBatchV1>
    {
        public Task Consume(ConsumeContext<ClassicCityHouseholdFinancialStressBatchV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                messageId: context.MessageId,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            ClassicCityHouseholdFinancialStressBatchV1 message,
            Guid? messageId,
            CancellationToken cancellationToken)
        {
            if (messageId is null)
                throw new InvalidOperationException(
                    "ClassicCityHouseholdFinancialStress message must have a MessageId.");

            ApplyCityHouseholdFinancialStressResult result = await mediator.Send(
                request: new ApplyCityHouseholdFinancialStressCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: messageId.Value,
                    ConsumerName: ClassicCityHouseholdFinancialStressConsumerDefinition.EndpointNameValue,
                    OccurredAtUtc: message.OccurredAtUtc,
                    Households: message.Households
                       .Select(x => new HouseholdFinancialStressSnapshotInput(
                            HouseholdExternalReferenceCode: x.HouseholdExternalReferenceCode,
                            OverdueObligationCount: x.OverdueObligationCount,
                            OverdueRentCount: x.OverdueRentCount,
                            OverdueUtilityCount: x.OverdueUtilityCount,
                            ArrearsObligationCount: x.ArrearsObligationCount,
                            ServiceCutoffCount: x.ServiceCutoffCount,
                            EvictionNoticeCount: x.EvictionNoticeCount,
                            EvictionEligibleCount: x.EvictionEligibleCount,
                            OldestOverdueAgeDays: x.OldestOverdueAgeDays,
                            TotalOverdueAmount: x.TotalOverdueAmount,
                            DistressScore: x.DistressScore))
                       .ToArray()),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case ApplyCityHouseholdFinancialStressStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied classic city household financial stress batch for cityId={CityId}, messageId={MessageId}, households={Households}, batch={BatchNumber}/{TotalBatches}.",
                        message.CityId,
                        messageId,
                        result.AppliedHouseholdCount,
                        message.BatchNumber,
                        message.TotalBatches);
                    break;

                case ApplyCityHouseholdFinancialStressStatus.Duplicate:
                    logger.LogDebug(
                        message:
                        "Skipped duplicate classic city household financial stress batch for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case ApplyCityHouseholdFinancialStressStatus.CityDeleted:
                    logger.LogDebug(
                        message:
                        "Skipped classic city household financial stress batch for deleted cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case ApplyCityHouseholdFinancialStressStatus.CityArchived:
                    logger.LogDebug(
                        message:
                        "Skipped classic city household financial stress batch for archived cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;
            }
        }
    }
}
