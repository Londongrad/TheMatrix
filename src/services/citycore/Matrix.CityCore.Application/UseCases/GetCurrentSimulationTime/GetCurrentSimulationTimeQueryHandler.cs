using Matrix.CityCore.Application.Abstractions;
using Matrix.CityCore.Domain.Aggregates;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.GetCurrentSimulationTime
{
    public sealed class GetCurrentSimulationTimeQueryHandler(ICityClockRepository clockRepository)
        : IRequestHandler<GetCurrentSimulationTimeQuery, SimulationTimeDto>
    {
        private readonly ICityClockRepository _clockRepository = clockRepository;

        public async Task<SimulationTimeDto> Handle(
            GetCurrentSimulationTimeQuery request,
            CancellationToken cancellationToken)
        {
            var clock = await _clockRepository.GetAsync(cancellationToken);

            // если часов ещё нет – вернем дефолтное состояние
            clock ??= CityClock.CreateDefault();

            return new SimulationTimeDto(
                CurrentTime: clock.Time.Current,
                SimMinutesPerTick: clock.SimMinutesPerTick,
                IsPaused: clock.IsPaused);
        }
    }
}
