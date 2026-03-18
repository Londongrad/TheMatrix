using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence.Seed
{
    public sealed class DefaultUserAccessPolicySeeder(IdentityDbContext db)
    {
        private readonly IdentityDbContext _db = db;

        public async Task SeedAsync(CancellationToken cancellationToken)
        {
            bool exists = await _db.DefaultUserAccessPolicies
               .AsNoTracking()
               .AnyAsync(
                    predicate: x => x.Id == DefaultUserAccessPolicy.SingletonId,
                    cancellationToken: cancellationToken);

            if (exists)
                return;

            DefaultUserAccessPolicy policy = DefaultUserAccessPolicy.CreateDefault(DateTime.UtcNow);
            await _db.DefaultUserAccessPolicies.AddAsync(
                entity: policy,
                cancellationToken: cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
