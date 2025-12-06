using Matrix.Identity.Domain.ValueObjects;

namespace Matrix.Identity.Application.Abstractions
{
    public interface IGeoLocationService
    {
        Task<GeoLocation?> ResolveAsync(
            string ipAddress,
            CancellationToken cancellationToken = default);
    }
}
