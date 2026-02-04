using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Application.Abstractions.Outbox;
using Matrix.CityCore.Application.Abstractions.Persistence;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Cities.Enums;
using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.UpdateCityEnvironment
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

            ClimateZone climateZone = GuardHelper.AgainstInvalidStringToEnum<ClimateZone>(
                value: request.ClimateZone,
                propertyName: nameof(request.ClimateZone));

            Hemisphere hemisphere = GuardHelper.AgainstInvalidStringToEnum<Hemisphere>(
                value: request.Hemisphere,
                propertyName: nameof(request.Hemisphere));

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
