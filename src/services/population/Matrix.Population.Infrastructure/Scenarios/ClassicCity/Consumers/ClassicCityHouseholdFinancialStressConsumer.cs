using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
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
        public async Task Consume(ConsumeContext<ClassicCityHouseholdFinancialStressBatchV1> context)
        {
            if (context.MessageId is null)
                throw new InvalidOperationException(
                    "ClassicCityHouseholdFinancialStress message must have a MessageId.");

            ClassicCityHouseholdFinancialStressBatchV1 message = context.Message;

            ApplyCityHouseholdFinancialStressResult result = await mediator.Send(
                request: new ApplyCityHouseholdFinancialStressCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: context.MessageId.Value,
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
                cancellationToken: context.CancellationToken);

            switch (result.Status)
            {
                case ApplyCityHouseholdFinancialStressStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied classic city household financial stress batch for cityId={CityId}, messageId={MessageId}, households={Households}, batch={BatchNumber}/{TotalBatches}.",
                        message.CityId,
                        context.MessageId,
                        result.AppliedHouseholdCount,
                        message.BatchNumber,
                        message.TotalBatches);
                    break;

                case ApplyCityHouseholdFinancialStressStatus.Duplicate:
                    logger.LogDebug(
                        message:
                        "Skipped duplicate classic city household financial stress batch for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case ApplyCityHouseholdFinancialStressStatus.CityDeleted:
                    logger.LogDebug(
                        message:
                        "Skipped classic city household financial stress batch for deleted cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case ApplyCityHouseholdFinancialStressStatus.CityArchived:
                    logger.LogDebug(
                        message:
                        "Skipped classic city household financial stress batch for archived cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;
            }
        }
    }
}
