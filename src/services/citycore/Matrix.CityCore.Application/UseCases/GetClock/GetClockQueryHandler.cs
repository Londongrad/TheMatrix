using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Time;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.GetClock
{
    public sealed class GetClockQueryHandler(ISimulationClockRepository repository)
        : IRequestHandler<GetClockQuery, ClockDto?>
    {
        public async Task<ClockDto?> Handle(
            GetClockQuery request,
            CancellationToken cancellationToken)
        {
            SimulationClock? clock = await repository.GetByCityIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            return clock is null
                ? null
                : ClockDto.FromDomain(clock);
        }
    }
}
