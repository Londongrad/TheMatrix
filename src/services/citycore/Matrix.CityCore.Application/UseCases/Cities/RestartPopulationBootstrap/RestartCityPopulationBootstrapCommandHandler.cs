using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.RestartPopulationBootstrap
{
    public sealed class RestartCityPopulationBootstrapCommandHandler(
        ICityRepository cityRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<RestartCityPopulationBootstrapCommand, RestartCityPopulationBootstrapResult>
    {
        public async Task<RestartCityPopulationBootstrapResult> Handle(
            RestartCityPopulationBootstrapCommand request,
            CancellationToken cancellationToken)
        {
            City? city = await cityRepository.GetByIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            if (city is null)
                return RestartCityPopulationBootstrapResult.NotFound();

            if (city.IsArchived || !city.HasPopulationBootstrapFailure)
                return RestartCityPopulationBootstrapResult.NotAllowed();

            bool restarted = city.TryRestartPopulationBootstrap(
                restartedAtUtc: DateTimeOffset.UtcNow,
                operationId: out Guid operationId);

            if (!restarted)
                return RestartCityPopulationBootstrapResult.NotAllowed();

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return RestartCityPopulationBootstrapResult.Restarted(operationId);
        }
    }
}
