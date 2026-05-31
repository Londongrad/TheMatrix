using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Events;
using Matrix.SimulationCore.Application.Abstractions.Outbox;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Bootstrap;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Topology;
using Matrix.SimulationCore.Application.Services.Bootstrap.Abstractions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity
{
    public sealed class CreateCityCommandHandler(
        ISimulationInstanceRepository simulationInstanceRepository,
        ICityRepository cityRepository,
        IDistrictRepository districtRepository,
        IResidentialBuildingRepository residentialBuildingRepository,
        ICityAnchorRepository cityAnchorRepository,
        IRoadNodeRepository roadNodeRepository,
        IRoadSegmentRepository roadSegmentRepository,
        ICityWeatherRepository cityWeatherRepository,
        ISimulationClockRepository clockRepository,
        ICitySimulationBootstrapStrategy simulationBootstrapStrategy,
        ISimulationCoreOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork) : IRequestHandler<CreateCityCommand, CityCreatedDto>
    {
        public async Task<CityCreatedDto> Handle(
            CreateCityCommand request,
            CancellationToken cancellationToken)
        {
            if (request.ProvisioningCorrelationId.HasValue)
            {
                CityCreatedDto? existing = await TryGetExistingByProvisioningCorrelationAsync(
                    provisioningCorrelationId: request.ProvisioningCorrelationId.Value,
                    cancellationToken: cancellationToken);

                if (existing is not null)
                    return existing;
            }

            ClassicCityBootstrapPlan bootstrapPlan = simulationBootstrapStrategy.CreatePlan(request);

            City city = bootstrapPlan.City;
            SimulationInstance instance = bootstrapPlan.Instance;
            CityTopologySeed topology = bootstrapPlan.Topology;
            SimulationClock clock = bootstrapPlan.Clock;

            try
            {
                await unitOfWork.ExecuteInTransactionAsync(
                    action: async ct =>
                    {
                        await simulationInstanceRepository.AddAsync(
                            instance: instance,
                            cancellationToken: ct);
                        await cityRepository.AddAsync(
                            city: city,
                            cancellationToken: ct);
                        await districtRepository.AddRangeAsync(
                            districts: topology.Districts,
                            cancellationToken: ct);
                        await residentialBuildingRepository.AddRangeAsync(
                            buildings: topology.ResidentialBuildings,
                            cancellationToken: ct);
                        await cityAnchorRepository.AddRangeAsync(
                            anchors: topology.Anchors,
                            cancellationToken: ct);
                        await roadNodeRepository.AddRangeAsync(
                            roadNodes: topology.RoadNodes,
                            cancellationToken: ct);
                        await roadSegmentRepository.AddRangeAsync(
                            roadSegments: topology.RoadSegments,
                            cancellationToken: ct);
                        if (bootstrapPlan.Weather is not null)
                            await cityWeatherRepository.AddAsync(
                                cityWeather: bootstrapPlan.Weather,
                                cancellationToken: ct);
                        await clockRepository.AddAsync(
                            clock: clock,
                            cancellationToken: ct);
                        await DomainEventDispatchHelper.PublishAndClearAsync(
                            source: instance,
                            publish: outboxWriter.AddSimulationEventsAsync,
                            cancellationToken: ct);
                        await DomainEventDispatchHelper.PublishAndClearAsync(
                            source: city,
                            publish: outboxWriter.AddCityEventsAsync,
                            cancellationToken: ct);
                        if (bootstrapPlan.Weather is not null)
                            await DomainEventDispatchHelper.PublishAndClearAsync(
                                source: bootstrapPlan.Weather,
                                publish: outboxWriter.AddWeatherEventsAsync,
                                cancellationToken: ct);
                        await unitOfWork.SaveChangesAsync(ct);
                    },
                    cancellationToken: cancellationToken);
            }
            catch when (request.ProvisioningCorrelationId.HasValue)
            {
                CityCreatedDto? existing = await TryGetExistingByProvisioningCorrelationAsync(
                    provisioningCorrelationId: request.ProvisioningCorrelationId.Value,
                    cancellationToken: cancellationToken);

                if (existing is not null)
                    return existing;

                throw;
            }

            return MapToCreatedDto(city);
        }

        private async Task<CityCreatedDto?> TryGetExistingByProvisioningCorrelationAsync(
            Guid provisioningCorrelationId,
            CancellationToken cancellationToken)
        {
            City? existing = await cityRepository.GetByProvisioningCorrelationIdAsync(
                provisioningCorrelationId: provisioningCorrelationId,
                cancellationToken: cancellationToken);

            return existing is null
                ? null
                : MapToCreatedDto(existing);
        }

        private static CityCreatedDto MapToCreatedDto(City city)
        {
            return new CityCreatedDto(
                CityId: city.Id.Value,
                PopulationBootstrapOperationId: city.PopulationBootstrapOperationId,
                EconomyBootstrapOperationId: city.EconomyBootstrapOperationId,
                SimulationKind: SimulationKind.ClassicCity.ToString());
        }
    }
}
