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
        IUnitOfWork unitOfWork) : IRequestHandler<CreateCityCommand, CityCreatedDto>
    {
        public async Task<CityCreatedDto> Handle(
            CreateCityCommand request,
            CancellationToken cancellationToken)
        {
            ClimateZone climateZone = GuardHelper.AgainstInvalidStringToEnum<ClimateZone>(
                value: request.ClimateZone,
                propertyName: nameof(request.ClimateZone));

            Hemisphere hemisphere = GuardHelper.AgainstInvalidStringToEnum<Hemisphere>(
                value: request.Hemisphere,
                propertyName: nameof(request.Hemisphere));

            CitySizeTier sizeTier = ParseOrDefault(
                value: request.SizeTier,
                defaultValue: CitySizeTier.Medium,
                propertyName: nameof(request.SizeTier));

            UrbanDensity urbanDensity = ParseOrDefault(
                value: request.UrbanDensity,
                defaultValue: UrbanDensity.Balanced,
                propertyName: nameof(request.UrbanDensity));

            CityDevelopmentLevel developmentLevel = ParseOrDefault(
                value: request.DevelopmentLevel,
                defaultValue: CityDevelopmentLevel.Balanced,
                propertyName: nameof(request.DevelopmentLevel));

            var environment = CityEnvironment.Create(
                climateZone: climateZone,
                hemisphere: hemisphere,
                utcOffset: CityUtcOffset.FromMinutes(request.UtcOffsetMinutes));

            var generationProfile = CityGenerationProfile.Create(
                sizeTier: sizeTier,
                urbanDensity: urbanDensity,
                developmentLevel: developmentLevel);

            string effectiveSeed = string.IsNullOrWhiteSpace(request.GenerationSeed)
                ? BuildDefaultGenerationSeed(
                    name: request.Name,
                    climateZone: climateZone,
                    hemisphere: hemisphere,
                    utcOffsetMinutes: request.UtcOffsetMinutes,
                    generationProfile: generationProfile)
                : request.GenerationSeed;

            var generationSeed = new CityGenerationSeed(effectiveSeed);

            var startSimTime = SimTime.FromUtc(request.StartSimTimeUtc);

            var city = City.Create(
                name: new CityName(request.Name),
                environment: environment,
                generationSeed: generationSeed,
                generationProfile: generationProfile,
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
                    await outboxWriter.AddCityEventsAsync(
                        domainEvents: city.DomainEvents,
                        cancellationToken: ct);
                    await outboxWriter.AddWeatherEventsAsync(
                        domainEvents: cityWeather.DomainEvents,
                        cancellationToken: ct);

                    city.ClearDomainEvents();
                    cityWeather.ClearDomainEvents();
                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);

            return new CityCreatedDto(
                CityId: city.Id.Value,
                PopulationBootstrapOperationId: city.PopulationBootstrapOperationId);
        }

        private static TEnum ParseOrDefault<TEnum>(
            string? value,
            TEnum defaultValue,
            string propertyName)
            where TEnum : struct, Enum
        {
            return string.IsNullOrWhiteSpace(value)
                ? defaultValue
                : GuardHelper.AgainstInvalidStringToEnum<TEnum>(
                    value: value,
                    propertyName: propertyName);
        }

        private static string BuildDefaultGenerationSeed(
            string name,
            ClimateZone climateZone,
            Hemisphere hemisphere,
            int utcOffsetMinutes,
            CityGenerationProfile generationProfile)
        {
            return string.Concat(
                name.Trim(),
                "|",
                climateZone,
                "|",
                hemisphere,
                "|",
                utcOffsetMinutes,
                "|",
                generationProfile.SizeTier,
                "|",
                generationProfile.UrbanDensity,
                "|",
                generationProfile.DevelopmentLevel);
        }
    }
}
