using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.Common;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityEnvironment
{
    public sealed class SyncCityEnvironmentCommandHandler(
        ICityPopulationArchiveStateRepository cityPopulationArchiveStateRepository,
        ICityPopulationDeletionStateRepository cityPopulationDeletionStateRepository,
        ICityPopulationEnvironmentRepository cityPopulationEnvironmentRepository,
        IUnitOfWork unitOfWork)
        : IRequestHandler<SyncCityEnvironmentCommand, SyncCityEnvironmentResult>,
            IRequestHandler<ApplyCityEnvironmentSyncCommand, SyncCityEnvironmentResult>
    {
        public async Task<SyncCityEnvironmentResult> Handle(
            ApplyCityEnvironmentSyncCommand request,
            CancellationToken cancellationToken)
        {
            return await HandleInternal(
                cityIdValue: request.CityId,
                climateZone: request.ClimateZone,
                hemisphere: request.Hemisphere,
                utcOffsetMinutes: request.UtcOffsetMinutes,
                syncedAtUtcValue: request.SyncedAtUtc,
                cancellationToken: cancellationToken);
        }

        public async Task<SyncCityEnvironmentResult> Handle(
            SyncCityEnvironmentCommand request,
            CancellationToken cancellationToken)
        {
            return await HandleInternal(
                cityIdValue: request.CityId,
                climateZone: request.ClimateZone,
                hemisphere: request.Hemisphere,
                utcOffsetMinutes: request.UtcOffsetMinutes,
                syncedAtUtcValue: request.SyncedAtUtc,
                cancellationToken: cancellationToken);
        }

        private async Task<SyncCityEnvironmentResult> HandleInternal(
            Guid cityIdValue,
            string climateZone,
            string hemisphere,
            int utcOffsetMinutes,
            DateTimeOffset? syncedAtUtcValue,
            CancellationToken cancellationToken)
        {
            var cityId = CityId.From(cityIdValue);
            DateTimeOffset syncedAtUtc = syncedAtUtcValue ?? DateTimeOffset.UtcNow;

            var input = new CityPopulationEnvironmentInput(
                ClimateZone: climateZone,
                Hemisphere: hemisphere,
                UtcOffsetMinutes: utcOffsetMinutes);

            return await unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    CityPopulationEnvironment? environment = await cityPopulationEnvironmentRepository.GetByCityAsync(
                        cityId: cityId,
                        cancellationToken: ct);
                    CityPopulationDeletionState? deletionState =
                        await cityPopulationDeletionStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct);

                    if (deletionState is not null)
                        return new SyncCityEnvironmentResult(SyncCityEnvironmentStatus.CityDeleted);

                    CityPopulationArchiveState? archiveState =
                        await cityPopulationArchiveStateRepository.GetByCityAsync(
                            cityId: cityId,
                            cancellationToken: ct);

                    if (archiveState is not null)
                        return new SyncCityEnvironmentResult(SyncCityEnvironmentStatus.CityArchived);

                    if (environment is null)
                    {
                        CityPopulationEnvironment newEnvironment = CityPopulationEnvironmentMapper.Create(
                            cityId: cityIdValue,
                            input: input,
                            createdAtUtc: syncedAtUtc);

                        await cityPopulationEnvironmentRepository.AddAsync(
                            environment: newEnvironment,
                            cancellationToken: ct);

                        await unitOfWork.SaveChangesAsync(ct);

                        return new SyncCityEnvironmentResult(SyncCityEnvironmentStatus.Applied);
                    }

                    if (syncedAtUtc < environment.UpdatedAtUtc)
                        return new SyncCityEnvironmentResult(SyncCityEnvironmentStatus.Stale);

                    if (syncedAtUtc == environment.UpdatedAtUtc &&
                        environment.ClimateZone ==
                        CityPopulationEnvironmentMapper.ParseClimateZone(input.ClimateZone) &&
                        environment.Hemisphere == CityPopulationEnvironmentMapper.ParseHemisphere(input.Hemisphere) &&
                        environment.UtcOffsetMinutes == input.UtcOffsetMinutes)
                        return new SyncCityEnvironmentResult(SyncCityEnvironmentStatus.Duplicate);

                    CityPopulationEnvironmentMapper.Sync(
                        environment: environment,
                        input: input,
                        updatedAtUtc: syncedAtUtc);

                    await unitOfWork.SaveChangesAsync(ct);

                    return new SyncCityEnvironmentResult(SyncCityEnvironmentStatus.Applied);
                },
                cancellationToken: cancellationToken);
        }
    }
}
