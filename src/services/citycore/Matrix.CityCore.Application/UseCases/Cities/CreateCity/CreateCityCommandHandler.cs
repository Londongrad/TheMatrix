using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Cities.Enums;
using Matrix.CityCore.Domain.Simulation;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.CreateCity
{
    public sealed class CreateCityCommandHandler(
        ICityRepository cityRepository,
        ISimulationClockRepository clockRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<CreateCityCommand, Guid>
    {
        public async Task<Guid> Handle(
            CreateCityCommand request,
            CancellationToken cancellationToken)
        {
            ClimateZone climateZone = GuardHelper.AgainstInvalidStringToEnum<ClimateZone>(
                value: request.ClimateZone,
                propertyName: nameof(request.ClimateZone));

            Hemisphere hemisphere = GuardHelper.AgainstInvalidStringToEnum<Hemisphere>(
                value: request.Hemisphere,
                propertyName: nameof(request.Hemisphere));

            CityEnvironment environment = CityEnvironment.Create(
                climateZone: climateZone,
                hemisphere: hemisphere,
                utcOffset: CityUtcOffset.FromMinutes(request.UtcOffsetMinutes));

            var city = City.Create(
                name: new CityName(request.Name),
                environment: environment,
                createdAtUtc: DateTimeOffset.UtcNow);

            SimSpeed speed = request.SpeedMultiplier == 1.0m
                ? SimSpeed.RealTime()
                : SimSpeed.From(request.SpeedMultiplier);

            var clock = SimulationClock.Create(
                cityId: city.Id,
                startTime: SimTime.FromUtc(request.StartSimTimeUtc),
                speed: speed);

            await unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    await cityRepository.AddAsync(
                        city: city,
                        cancellationToken: ct);
                    await clockRepository.AddAsync(
                        clock: clock,
                        cancellationToken: ct);
                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);

            return city.Id.Value;
        }
    }
}