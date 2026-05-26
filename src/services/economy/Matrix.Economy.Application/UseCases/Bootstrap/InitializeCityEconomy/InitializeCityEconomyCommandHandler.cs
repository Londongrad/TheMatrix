using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Errors;
using MediatR;

namespace Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy
{
    public sealed class InitializeCityEconomyCommandHandler(
        ICityEconomyBootstrapService cityEconomyBootstrapService,
        ICityEconomyDeletionRepository deletionRepository)
        : IRequestHandler<InitializeCityEconomyCommand, CityEconomyBootstrapResultDto>
    {
        public async Task<CityEconomyBootstrapResultDto> Handle(
            InitializeCityEconomyCommand request,
            CancellationToken cancellationToken)
        {
            if (await deletionRepository.GetDeletedAtUtcAsync(
                    cityId: request.CityId,
                    cancellationToken: cancellationToken) is not null)
                throw EconomyApplicationErrorsFactory.CannotInitializeDeletedCity(request.CityId);

            return await cityEconomyBootstrapService.BootstrapAsync(
                cityId: request.CityId,
                simulationKind: request.SimulationKind,
                economyProfile: request.EconomyProfile,
                createdAtUtc: request.CreatedAtUtc,
                cancellationToken: cancellationToken);
        }
    }
}
