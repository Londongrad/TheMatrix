using Matrix.Resources.Domain.Scenarios.ClassicCity.Systems;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.Common
{
    public sealed record PendingResupplyDto(
        string Focus,
        string Intensity,
        Guid? FocusDistrictId,
        long ReadyAtTickId)
    {
        public static PendingResupplyDto? FromDomain(CityPendingResupplyState state)
        {
            return state.IsScheduled
                ? new PendingResupplyDto(
                    Focus: state.Focus,
                    Intensity: state.Intensity,
                    FocusDistrictId: state.FocusDistrictId,
                    ReadyAtTickId: state.ReadyAtTickId)
                : null;
        }
    }
}
