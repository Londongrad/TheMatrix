using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Application.Abstractions.Outbox;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Application.Services.Bootstrap;
using Matrix.CityCore.Application.Services.Bootstrap.Abstractions;
using Matrix.CityCore.Application.Services.Topology;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Cities.Enums;
using Matrix.CityCore.Domain.Simulation;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.CreateCity
{
    public sealed class CreateCityCommandHandler(
        ICityRepository cityRepository,
        IDistrictRepository districtRepository,
        IResidentialBuildingRepository residentialBuildingRepository,
        ICityWeatherRepository cityWeatherRepository,
        ISimulationClockRepository clockRepository,
        IEnumerable<ICitySimulationBootstrapStrategy> simulationBootstrapStrategies,
        ICityCoreOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork) : IRequestHandler<CreateCityCommand, CityCreatedDto>
    {
        public async Task<CityCreatedDto> Handle(
            CreateCityCommand request,
            CancellationToken cancellationToken)
        {
            SimulationKind simulationKind = ParseSimulationKind(request.SimulationKind);
            ICitySimulationBootstrapStrategy bootstrapStrategy = ResolveBootstrapStrategy(
                simulationBootstrapStrategies: simulationBootstrapStrategies,
                simulationKind: simulationKind);
            CitySimulationBootstrapPlan bootstrapPlan = bootstrapStrategy.CreatePlan(request);

            City city = bootstrapPlan.City;
            CityTopologySeed topology = bootstrapPlan.Topology;
            SimulationClock clock = bootstrapPlan.Clock;

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
                    if (bootstrapPlan.Weather is not null)
                        await cityWeatherRepository.AddAsync(
                            cityWeather: bootstrapPlan.Weather,
                            cancellationToken: ct);
                    await clockRepository.AddAsync(
                        clock: clock,
                        cancellationToken: ct);
                    await outboxWriter.AddCityEventsAsync(
                        domainEvents: city.DomainEvents,
                        cancellationToken: ct);
                    if (bootstrapPlan.Weather is not null)
                        await outboxWriter.AddWeatherEventsAsync(
                            domainEvents: bootstrapPlan.Weather.DomainEvents,
                            cancellationToken: ct);

                    city.ClearDomainEvents();
                    bootstrapPlan.Weather?.ClearDomainEvents();
                    await unitOfWork.SaveChangesAsync(ct);
                },
                cancellationToken: cancellationToken);

            return new CityCreatedDto(
                CityId: city.Id.Value,
                PopulationBootstrapOperationId: city.PopulationBootstrapOperationId,
                SimulationKind: city.SimulationKind.ToString());
        }

        private static SimulationKind ParseSimulationKind(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? SimulationKind.ClassicCity
                : GuardHelper.AgainstInvalidStringToEnum<SimulationKind>(
                    value: value,
                    propertyName: nameof(CreateCityCommand.SimulationKind));
        }

        private static ICitySimulationBootstrapStrategy ResolveBootstrapStrategy(
            IEnumerable<ICitySimulationBootstrapStrategy> simulationBootstrapStrategies,
            SimulationKind simulationKind)
        {
            ICitySimulationBootstrapStrategy? strategy = simulationBootstrapStrategies.SingleOrDefault(
                x => x.Kind == simulationKind);

            return strategy ?? throw new InvalidOperationException(
                $"City simulation bootstrap strategy is not registered for kind '{simulationKind}'.");
        }
    }
}
