using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Abstractions;
using Matrix.Population.Application.UseCases.Population.Common;
using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.UseCases.Population.SyncCityEnvironment
{
    public sealed class SyncCityEnvironmentCommandHandler(
        ICityPopulationEnvironmentRepository cityPopulationEnvironmentRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<SyncCityEnvironmentCommand>
    {
        public async Task Handle(
            SyncCityEnvironmentCommand request,
            CancellationToken cancellationToken)
        {
            if (request.CityId == Guid.Empty)
                throw new ArgumentException("CityId cannot be empty.", nameof(request.CityId));

            ArgumentException.ThrowIfNullOrWhiteSpace(request.ClimateZone);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Hemisphere);

            CityId cityId = CityId.From(request.CityId);
            var input = new CityPopulationEnvironmentInput(
                ClimateZone: request.ClimateZone,
                Hemisphere: request.Hemisphere,
                UtcOffsetMinutes: request.UtcOffsetMinutes);

            await unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    DateTimeOffset updatedAtUtc = DateTimeOffset.UtcNow;

                    CityPopulationEnvironment? environment = await cityPopulationEnvironmentRepository.GetByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);

                    if (environment is null)
                    {
                        CityPopulationEnvironment newEnvironment = CityPopulationEnvironmentMapper.Create(
                            cityId: request.CityId,
                            input: input,
                            createdAtUtc: updatedAtUtc);

                        await cityPopulationEnvironmentRepository.AddAsync(
                            environment: newEnvironment,
                            cancellationToken: ct);
                    }
                    else
                    {
                        CityPopulationEnvironmentMapper.Sync(
                            environment: environment,
                            input: input,
                            updatedAtUtc: updatedAtUtc);
                    }

                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);
        }
    }
}
