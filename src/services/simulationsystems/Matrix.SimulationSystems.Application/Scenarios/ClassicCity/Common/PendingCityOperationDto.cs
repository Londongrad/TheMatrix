using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Common
{
    public sealed record PendingCityOperationDto(
        string Focus,
        string Intensity,
        long ReadyAtTickId)
    {
        public static PendingCityOperationDto? FromDomain(CityPendingOperationalWorkState state)
        {
            return state.IsScheduled
                ? new PendingCityOperationDto(
                    Focus: state.Focus,
                    Intensity: state.Intensity,
                    ReadyAtTickId: state.ReadyAtTickId)
                : null;
        }
    }
}
