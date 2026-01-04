using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Identity.Infrastructure.Persistence
{
    public class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<OneTimeToken> OneTimeTokens => Set<OneTimeToken>();

        public DbSet<Permission> Permissions => Set<Permission>();

        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();

        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<UserPermissionOverride> UserPermissionOverrides => Set<UserPermissionOverride>();

        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        }
    }
}
