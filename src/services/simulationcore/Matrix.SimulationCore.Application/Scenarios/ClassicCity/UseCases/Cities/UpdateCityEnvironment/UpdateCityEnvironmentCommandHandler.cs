using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Events;
using Matrix.SimulationCore.Application.Abstractions.Outbox;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.UpdateCityEnvironment
{
    public sealed class UpdateCityEnvironmentCommandHandler(
        ICityRepository cityRepository,
        ISimulationCoreOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork) : IRequestHandler<UpdateCityEnvironmentCommand, bool>
    {
        public async Task<bool> Handle(
            UpdateCityEnvironmentCommand request,
            CancellationToken cancellationToken)
        {
            City? city = await cityRepository.GetByIdAsync(
                cityId: new CityId(request.CityId),
                cancellationToken: cancellationToken);

            if (city is null)
                return false;

            ClimateZone climateZone = Enum.Parse<ClimateZone>(
                value: request.ClimateZone,
                ignoreCase: true);

            Hemisphere hemisphere = Enum.Parse<Hemisphere>(
                value: request.Hemisphere,
                ignoreCase: true);

            var environment = CityEnvironment.Create(
                climateZone: climateZone,
                hemisphere: hemisphere,
                utcOffset: CityUtcOffset.FromMinutes(request.UtcOffsetMinutes));

            city.ChangeEnvironment(environment);
            await DomainEventDispatchHelper.PublishAndClearAsync(
                source: city,
                publish: outboxWriter.AddCityEventsAsync,
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
