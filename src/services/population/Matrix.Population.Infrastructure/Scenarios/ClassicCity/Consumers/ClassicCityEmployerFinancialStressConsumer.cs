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
        public Task Consume(ConsumeContext<ClassicCityEmployerFinancialStressBatchV1> context)
        {
            return ConsumeAsync(
                message: context.Message,
                messageId: context.MessageId,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            ClassicCityEmployerFinancialStressBatchV1 message,
            Guid? messageId,
            CancellationToken cancellationToken)
        {
            if (messageId is null)
                throw new InvalidOperationException(
                    "ClassicCityEmployerFinancialStress message must have a MessageId.");

            ApplyCityEmployerFinancialStressResult result = await mediator.Send(
                request: new ApplyCityEmployerFinancialStressCommand(
                    CityId: message.CityId,
                    IntegrationMessageId: messageId.Value,
                    ConsumerName: ClassicCityEmployerFinancialStressConsumerDefinition.EndpointNameValue,
                    OccurredAtUtc: message.OccurredAtUtc,
                    Employers: message.Employers
                       .Select(x => new EmployerFinancialStressSnapshotInput(
                            WorkplaceExternalReferenceCode: x.WorkplaceExternalReferenceCode,
                            RequestedGrossPayrollAmount: x.RequestedGrossPayrollAmount,
                            PaidGrossPayrollAmount: x.PaidGrossPayrollAmount,
                            MissedGrossPayrollAmount: x.MissedGrossPayrollAmount,
                            PayrollFulfillmentRatio: x.PayrollFulfillmentRatio,
                            FailedPayrollCount: x.FailedPayrollCount,
                            PartialPayrollCount: x.PartialPayrollCount,
                            CurrentBalanceAmount: x.CurrentBalanceAmount,
                            DistressScore: x.DistressScore,
                            HasHiringFreeze: x.HasHiringFreeze,
                            HasLayoffPressure: x.HasLayoffPressure))
                       .ToArray()),
                cancellationToken: cancellationToken);

            switch (result.Status)
            {
                case ApplyCityEmployerFinancialStressStatus.Applied:
                    logger.LogInformation(
                        message:
                        "Applied classic city employer financial stress batch for cityId={CityId}, messageId={MessageId}, employers={Employers}, batch={BatchNumber}/{TotalBatches}.",
                        message.CityId,
                        messageId,
                        result.AppliedEmployerCount,
                        message.BatchNumber,
                        message.TotalBatches);
                    break;

                case ApplyCityEmployerFinancialStressStatus.Duplicate:
                    logger.LogDebug(
                        message:
                        "Skipped duplicate classic city employer financial stress batch for cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case ApplyCityEmployerFinancialStressStatus.CityDeleted:
                    logger.LogDebug(
                        message:
                        "Skipped classic city employer financial stress batch for deleted cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;

                case ApplyCityEmployerFinancialStressStatus.CityArchived:
                    logger.LogDebug(
                        message:
                        "Skipped classic city employer financial stress batch for archived cityId={CityId}, messageId={MessageId}.",
                        message.CityId,
                        messageId);
                    break;
            }
        }
    }
}
