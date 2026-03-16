using Matrix.Economy.Application.Abstractions;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy
{
    public sealed class InitializeCityEconomyCommandHandler(
        ICityEconomyBootstrapService cityEconomyBootstrapService)
        : IRequestHandler<InitializeCityEconomyCommand, CityEconomyBootstrapResultDto>
    {
        public async Task<CityEconomyBootstrapResultDto> Handle(
            InitializeCityEconomyCommand request,
            CancellationToken cancellationToken)
        {
            return await cityEconomyBootstrapService.BootstrapAsync(
                cityId: request.CityId,
                simulationKind: request.SimulationKind,
                economyProfile: request.EconomyProfile,
                createdAtUtc: request.CreatedAtUtc,
                cancellationToken: cancellationToken);
        }
    }
}
