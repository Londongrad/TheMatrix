using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SetCityEmergencyRationing
{
    public sealed record SetCityEmergencyRationingCommand(
        Guid CityId,
        bool Enabled) : IRequest<SetCityEmergencyRationingResult>;
}
