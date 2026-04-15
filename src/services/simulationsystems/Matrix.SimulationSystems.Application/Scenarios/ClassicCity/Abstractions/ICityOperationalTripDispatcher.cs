namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityOperationalTripDispatcher
    {
        Task<bool> TryDispatchUtilityIncidentResponseAsync(
            Guid cityId,
            Guid focusDistrictId,
            string focus,
            string intensity,
            CancellationToken cancellationToken);
    }
}
