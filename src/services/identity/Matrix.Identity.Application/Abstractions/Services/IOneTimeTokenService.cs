using Matrix.Identity.Domain.Enums;

namespace Matrix.Identity.Application.Abstractions.Services
{
    public interface IOneTimeTokenService
    {
        /// <summary>
        ///     Generates a URL-safe raw token to be sent to the user (never stored).
        /// </summary>
        string GenerateRawToken();

        /// <summary>
        ///     Hashes raw token for storage/lookup.
        /// </summary>
        string HashToken(string rawToken);

        TimeSpan GetTtl(OneTimeTokenPurpose purpose);
    }
}
