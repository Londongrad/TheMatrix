namespace Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityResupplyTripDispatcher
    {
        Task<bool> TryDispatchDistrictResupplyAsync(
            Guid cityId,
            Guid focusDistrictId,
            string focus,
            string intensity,
            CancellationToken cancellationToken);
    }
}
