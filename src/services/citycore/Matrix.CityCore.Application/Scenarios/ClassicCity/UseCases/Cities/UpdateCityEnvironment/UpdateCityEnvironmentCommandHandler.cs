using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.CityCore.Application.Abstractions.Outbox;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.UpdateCityEnvironment
{
    public sealed class UpdateCityEnvironmentCommandHandler(
        ICityRepository cityRepository,
        ICityCoreOutboxWriter outboxWriter,
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
            await outboxWriter.AddCityEventsAsync(
                domainEvents: city.DomainEvents,
                cancellationToken: cancellationToken);
            city.ClearDomainEvents();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
