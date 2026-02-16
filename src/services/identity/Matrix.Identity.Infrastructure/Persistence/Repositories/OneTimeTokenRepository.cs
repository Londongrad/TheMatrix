using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Repositories
{
    public sealed class OneTimeTokenRepository(IdentityDbContext dbContext) : IOneTimeTokenRepository
    {
        private DbSet<OneTimeToken> Tokens => dbContext.OneTimeTokens;

        public async Task Add(
            OneTimeToken token,
            CancellationToken cancellationToken)
        {
            await Tokens.AddAsync(
                entity: token,
                cancellationToken: cancellationToken);
        }

        public async Task<OneTimeToken?> Find(
            Guid userId,
            OneTimeTokenPurpose purpose,
            string tokenHash,
            CancellationToken cancellationToken)
        {
            return await Tokens
               .FirstOrDefaultAsync(
                    predicate: token => token.UserId == userId &&
                                        token.Purpose == purpose &&
                                        token.TokenHash == tokenHash,
                    cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<OneTimeToken>> GetActive(
            Guid userId,
            OneTimeTokenPurpose purpose,
            DateTime nowUtc,
            CancellationToken cancellationToken)
        {
            return await Tokens
               .Where(token => token.UserId == userId)
               .Where(token => token.Purpose == purpose)
               .Where(token => token.UsedAtUtc == null)
               .Where(token => token.RevokedAtUtc == null)
               .Where(token => token.ExpiresAtUtc > nowUtc)
               .OrderByDescending(token => token.CreatedAtUtc)
               .ToListAsync(cancellationToken);
        }
    }
}
