using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompleteEconomyBootstrap
{
    public sealed class CompleteCityEconomyBootstrapCommandHandler(
        ICityRepository cityRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider) : IRequestHandler<CompleteCityEconomyBootstrapCommand, bool>
    {
        public async Task<bool> Handle(
            CompleteCityEconomyBootstrapCommand request,
            CancellationToken cancellationToken)
        {
            City? city = await cityRepository.GetByIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            if (city is null)
                return false;

            bool updated = city.TryCompleteEconomyBootstrap(
                operationId: request.OperationId,
                completedAtUtc: timeProvider.GetUtcNow());

            if (updated)
                await unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
