using MassTransit;
using Matrix.Education.Application.Progression.AdvanceEducationProgression;
using Matrix.SimulationCore.Contracts.Events;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity;
using Matrix.SimulationCore.Contracts.Scenarios.ClassicCity.Simulation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Matrix.Education.Integration.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityEducationProgressionConsumer(
        IMediator mediator,
        ILogger<ClassicCityEducationProgressionConsumer> logger)
        : IConsumer<SimulationTickPhaseReachedV1>
    {
        public Task Consume(ConsumeContext<SimulationTickPhaseReachedV1> context)
        {
            return ConsumeAsync(context.Message, context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            SimulationTickPhaseReachedV1 message,
            CancellationToken cancellationToken)
        {
            if (!ClassicCityRuntimeKeys.IsMatch(message.ScenarioKey, message.HostTypeKey) ||
                !string.Equals(
                    message.PhaseKey,
                    ClassicCityTickPhaseKeys.Projection,
                    StringComparison.Ordinal))
                return;

            AdvanceEducationProgressionResult result = await mediator.Send(
                ClassicCityEducationProgressionCommandMapper.Map(message),
                cancellationToken);

            switch (result.Status)
            {
                case AdvanceEducationProgressionStatus.Applied:
                    logger.LogInformation(
                        "Applied education progression for cityId={CityId}, tickId={TickId}, evaluated={Evaluated}, started={Started}, completed={Completed}, withdrawn={Withdrawn}.",
                        message.HostId,
                        message.TickId,
                        result.BatchResult.StudentProfilesEvaluated,
                        result.BatchResult.EnrollmentsStarted,
                        result.BatchResult.EnrollmentsCompleted,
                        result.BatchResult.EnrollmentsWithdrawn);
                    break;
                case AdvanceEducationProgressionStatus.Duplicate:
                    logger.LogDebug(
                        "Skipped duplicate education progression for cityId={CityId}, tickId={TickId}.",
                        message.HostId,
                        message.TickId);
                    break;
                case AdvanceEducationProgressionStatus.OutOfOrder:
                    logger.LogWarning(
                        "Skipped out-of-order education progression for cityId={CityId}, tickId={TickId}.",
                        message.HostId,
                        message.TickId);
                    break;
                case AdvanceEducationProgressionStatus.SimulationDeleted:
                    logger.LogDebug(
                        "Skipped education progression for deleted cityId={CityId}, tickId={TickId}.",
                        message.HostId,
                        message.TickId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result.Status), result.Status, null);
            }
        }
    }
}
