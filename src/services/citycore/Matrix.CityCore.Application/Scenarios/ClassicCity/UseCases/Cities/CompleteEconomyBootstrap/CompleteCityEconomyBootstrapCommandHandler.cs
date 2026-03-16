using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompleteEconomyBootstrap
{
    public sealed class CompleteCityEconomyBootstrapCommandHandler(
        ICityRepository cityRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<CompleteCityEconomyBootstrapCommand, bool>
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
                completedAtUtc: DateTimeOffset.UtcNow);

            if (updated)
                await unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
