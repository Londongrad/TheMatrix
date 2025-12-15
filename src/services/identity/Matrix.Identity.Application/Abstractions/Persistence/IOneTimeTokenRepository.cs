using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;

namespace Matrix.Identity.Application.Abstractions.Persistence
{
    public interface IOneTimeTokenRepository
    {
        Task Add(
            OneTimeToken token,
            CancellationToken cancellationToken);

        Task<OneTimeToken?> Find(
            Guid userId,
            OneTimeTokenPurpose purpose,
            string tokenHash,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<OneTimeToken>> GetActive(
            Guid userId,
            OneTimeTokenPurpose purpose,
            DateTime nowUtc,
            CancellationToken cancellationToken);
    }
}
