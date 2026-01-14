using Matrix.CityCore.Application.Abstractions;
using Matrix.CityCore.Application.IntegrationEvents;
using Matrix.CityCore.Domain.Aggregates;
using Matrix.CityCore.Domain.Events;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.AdvanceSimulationTick
{
    public sealed class AdvanceSimulationTickCommandHandler(
        ICityClockRepository clockRepository,
        ICityCoreUnitOfWork unitOfWork,
        ICityIntegrationEventPublisher eventPublisher)
        : IRequestHandler<AdvanceSimulationTickCommand, Unit>
    {
        private readonly ICityClockRepository _clockRepository = clockRepository;
        private readonly ICityIntegrationEventPublisher _eventPublisher = eventPublisher;
        private readonly ICityCoreUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Unit> Handle(
            AdvanceSimulationTickCommand request,
            CancellationToken cancellationToken)
        {
            CityClock? clock = await _clockRepository.GetAsync(cancellationToken);

            if (clock is null)
            {
                clock = CityClock.CreateDefault();
                _clockRepository.Add(clock);
            }

            IReadOnlyCollection<ICityDomainEvent> domainEvents = clock.AdvanceOneTick();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Пока публикуем только "месяц закончился"
            foreach (ICityDomainEvent @event in domainEvents)
                if (@event is SimulationMonthEnded monthEnded)
                {
                    var integrationEvent = new SimulationMonthEndedIntegrationEvent(
                        EventId: Guid.NewGuid(),
                        OccurredAt: DateTimeOffset.UtcNow,
                        Year: monthEnded.Year,
                        Month: monthEnded.Month);

                    await _eventPublisher.PublishSimulationMonthEndedAsync(
                        integrationEvent: integrationEvent,
                        cancellationToken: cancellationToken);
                }

            return Unit.Value;
        }
    }
}
