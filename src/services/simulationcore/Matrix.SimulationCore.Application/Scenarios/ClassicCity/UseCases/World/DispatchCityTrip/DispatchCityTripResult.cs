using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.Common;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.DispatchCityTrip
{
    public sealed record DispatchCityTripResult(
        DispatchCityTripStatus Status,
        CityActiveTripDto? Trip,
        string? FailureReason);
}
