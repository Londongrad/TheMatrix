using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Identity.Infrastructure.Persistence.Configurations
{
    public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.ToTable("UserRoles");

            builder.HasKey(x => new
            {
                x.UserId,
                x.RoleId
            });

            builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Role>()
               .WithMany()
               .HasForeignKey(x => x.RoleId)
               .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.RoleId);
        }
    }
}
