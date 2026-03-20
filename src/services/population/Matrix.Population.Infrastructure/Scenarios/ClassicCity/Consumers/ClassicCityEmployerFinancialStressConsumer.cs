using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Population;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityEmployerFinancialStress;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityEmployerFinancialStressConsumer(
        IMediator mediator,
        ILogger<ClassicCityEmployerFinancialStressConsumer> logger)
        : IConsumer<ClassicCityEmployerFinancialStressBatchV1>
    {
        public async Task Consume(ConsumeContext<ClassicCityEmployerFinancialStressBatchV1> context)
        {
            if (context.MessageId is null)
                throw new InvalidOperationException(
                    "ClassicCityEmployerFinancialStress message must have a MessageId.");

            ClassicCityEmployerFinancialStressBatchV1 message = context.Message;

            ApplyCityEmployerFinancialStressResult result = await mediator.Send(
                request: new ApplyCityEmployerFinancialStressCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: context.MessageId.Value,
                    ConsumerName: ClassicCityEmployerFinancialStressConsumerDefinition.EndpointNameValue,
                    OccurredAtUtc: message.OccurredAtUtc,
                    Employers: message.Employers
                        .Select(x => new EmployerFinancialStressSnapshotInput(
                            x.WorkplaceExternalReferenceCode,
                            x.RequestedGrossPayrollAmount,
                            x.PaidGrossPayrollAmount,
                            x.MissedGrossPayrollAmount,
                            x.PayrollFulfillmentRatio,
                            x.FailedPayrollCount,
                            x.PartialPayrollCount,
                            x.CurrentBalanceAmount,
                            x.DistressScore,
                            x.HasHiringFreeze,
                            x.HasLayoffPressure))
                       .ToArray()),
                cancellationToken: context.CancellationToken);

            switch (result.Status)
            {
                case ApplyCityEmployerFinancialStressStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied classic city employer financial stress batch for cityId={CityId}, messageId={MessageId}, employers={Employers}, batch={BatchNumber}/{TotalBatches}.",
                        message.CityId,
                        context.MessageId,
                        result.AppliedEmployerCount,
                        message.BatchNumber,
                        message.TotalBatches);
                    break;

                case ApplyCityEmployerFinancialStressStatus.Duplicate:
                    logger.LogDebug(
                        message:
                        "Skipped duplicate classic city employer financial stress batch for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case ApplyCityEmployerFinancialStressStatus.CityDeleted:
                    logger.LogDebug(
                        message:
                        "Skipped classic city employer financial stress batch for deleted cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;

                case ApplyCityEmployerFinancialStressStatus.CityArchived:
                    logger.LogDebug(
                        message:
                        "Skipped classic city employer financial stress batch for archived cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        context.MessageId);
                    break;
            }
        }
    }
}
