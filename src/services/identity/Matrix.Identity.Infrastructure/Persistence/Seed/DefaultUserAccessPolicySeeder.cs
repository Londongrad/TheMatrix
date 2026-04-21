using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Seed
{
    public sealed class DefaultUserAccessPolicySeeder(
        IdentityDbContext db,
        IClock clock)
    {
        private readonly IdentityDbContext _db = db;
        private readonly IClock _clock = clock;

        public async Task SeedAsync(CancellationToken cancellationToken)
        {
            bool exists = await _db.DefaultUserAccessPolicies
               .AsNoTracking()
               .AnyAsync(
                    predicate: x => x.Id == DefaultUserAccessPolicy.SingletonId,
                    cancellationToken: cancellationToken);

            if (exists)
                return;

            DefaultUserAccessPolicy policy = DefaultUserAccessPolicy.CreateDefault(_clock.UtcNow);
            await _db.DefaultUserAccessPolicies.AddAsync(
                entity: policy,
                cancellationToken: cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
