using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories
{
    public class OneTimeTokenRepository : IOneTimeTokenRepository
    {
        public Task Add(
            OneTimeToken token,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<OneTimeToken?> Find(
            Guid userId,
            OneTimeTokenPurpose purpose,
            string tokenHash,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<OneTimeToken>> GetActive(
            Guid userId,
            OneTimeTokenPurpose purpose,
            DateTime nowUtc,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
