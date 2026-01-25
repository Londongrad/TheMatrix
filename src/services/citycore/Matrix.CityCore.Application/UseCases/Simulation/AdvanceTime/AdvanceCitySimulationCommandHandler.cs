using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Outbox;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Events.Simulation;
using Matrix.CityCore.Domain.Simulation;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.AdvanceTime
{
    public sealed class AdvanceCitySimulationCommandHandler(
        ISimulationClockRepository repository,
        ICityCoreOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork) : IRequestHandler<AdvanceCitySimulationCommand, bool>
    {
        public async Task<bool> Handle(
            AdvanceCitySimulationCommand request,
            CancellationToken cancellationToken)
        {
            SimulationClock? clock = await repository.GetByCityIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            if (clock is null)
                return false;

            await unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    clock.Advance(request.RealDelta);

                    SimulationTimeAdvancedDomainEvent? advancedEvent = clock.DomainEvents
                       .OfType<SimulationTimeAdvancedDomainEvent>()
                       .LastOrDefault();

                    if (advancedEvent is not null)
                        await outboxWriter.AddCityTimeAdvancedAsync(
                            cityId: advancedEvent.CityId,
                            from: advancedEvent.From,
                            to: advancedEvent.To,
                            tickId: advancedEvent.TickId,
                            speed: advancedEvent.Speed,
                            cancellationToken: ct);

                    clock.ClearDomainEvents();
                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);

            return true;
        }
    }
}
