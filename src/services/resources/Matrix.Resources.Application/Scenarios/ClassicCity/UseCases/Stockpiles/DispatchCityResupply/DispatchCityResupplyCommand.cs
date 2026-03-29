using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;
using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DispatchCityResupply
{
    public sealed record DispatchCityResupplyCommand(
        Guid CityId,
        ResupplyFocus Focus,
        ResupplyIntensity Intensity,
        bool EmergencyOverride) : IRequest<DispatchCityResupplyResult>;
}
