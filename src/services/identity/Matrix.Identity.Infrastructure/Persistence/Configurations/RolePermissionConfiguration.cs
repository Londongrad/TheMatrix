using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Identity.Infrastructure.Persistence.Configurations
{
    public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            builder.ToTable("RolePermissions");

            builder.HasKey(x => new
            {
                x.RoleId,
                x.PermissionKey
            });

            builder.Property(x => x.PermissionKey)
               .HasMaxLength(200)
               .IsRequired();

            builder.HasOne<Role>()
               .WithMany()
               .HasForeignKey(x => x.RoleId)
               .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Permission>()
               .WithMany()
               .HasForeignKey(x => x.PermissionKey)
               .HasPrincipalKey(x => x.Key)
               .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.PermissionKey);
        }
    }
}
