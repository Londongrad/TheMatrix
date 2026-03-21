using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationCore.Application.UseCases.Simulation.GetClock
{
    public sealed class GetClockQueryHandler(
        ISimulationClockRepository repository,
        ISimulationHostReadRepository simulationHostRepository)
        : IRequestHandler<GetClockQuery, ClockDto?>
    {
        public async Task<ClockDto?> Handle(
            GetClockQuery request,
            CancellationToken cancellationToken)
        {
            SimulationId simulationId = new(request.SimulationId);

            SimulationHost? host = await simulationHostRepository.GetBySimulationIdAsync(
                simulationId: simulationId,
                cancellationToken: cancellationToken);

            if (host is null)
                return null;

            SimulationClock? clock = await repository.GetBySimulationIdAsync(
                simulationId: simulationId,
                cancellationToken: cancellationToken);

            return clock is null
                ? null
                : ClockDto.FromDomain(
                    clock: clock,
                    host: host,
                    forcePaused: host.IsArchived);
        }
    }
}
