using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Application.Abstractions.Outbox;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Application.Services.Topology;
using Matrix.CityCore.Application.Services.Topology.Abstractions;
using Matrix.CityCore.Application.Services.Weather.Abstractions;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Cities.Enums;
using Matrix.CityCore.Domain.Simulation;
using Matrix.CityCore.Domain.Weather;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.CreateCity
{
    public sealed class CreateCityCommandHandler(
        ICityRepository cityRepository,
        IDistrictRepository districtRepository,
        IResidentialBuildingRepository residentialBuildingRepository,
        ICityWeatherRepository cityWeatherRepository,
        ISimulationClockRepository clockRepository,
        ICityTopologyBootstrapFactory cityTopologyBootstrapFactory,
        ICityWeatherBootstrapFactory cityWeatherBootstrapFactory,
        ICityCoreOutboxWriter outboxWriter,
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

            SimTime startSimTime = SimTime.FromUtc(request.StartSimTimeUtc);

            var city = City.Create(
                name: new CityName(request.Name),
                environment: environment,
                createdAtUtc: DateTimeOffset.UtcNow);

            CityTopologySeed topology = cityTopologyBootstrapFactory.CreateInitial(city);

            CityWeather cityWeather = cityWeatherBootstrapFactory.CreateInitial(
                city: city,
                initialTime: startSimTime);

            SimSpeed speed = request.SpeedMultiplier == 1.0m
                ? SimSpeed.RealTime()
                : SimSpeed.From(request.SpeedMultiplier);

            var clock = SimulationClock.Create(
                cityId: city.Id,
                startTime: startSimTime,
                speed: speed);

            await unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    await cityRepository.AddAsync(
                        city: city,
                        cancellationToken: ct);
                    await districtRepository.AddRangeAsync(
                        districts: topology.Districts,
                        cancellationToken: ct);
                    await residentialBuildingRepository.AddRangeAsync(
                        buildings: topology.ResidentialBuildings,
                        cancellationToken: ct);
                    await cityWeatherRepository.AddAsync(
                        cityWeather: cityWeather,
                        cancellationToken: ct);
                    await clockRepository.AddAsync(
                        clock: clock,
                        cancellationToken: ct);
                    await outboxWriter.AddWeatherEventsAsync(
                        domainEvents: cityWeather.DomainEvents,
                        cancellationToken: ct);

                    cityWeather.ClearDomainEvents();
                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);

            return city.Id.Value;
        }
    }
}