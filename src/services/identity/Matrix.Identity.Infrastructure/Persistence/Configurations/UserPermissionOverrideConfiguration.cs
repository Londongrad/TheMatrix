using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Identity.Infrastructure.Persistence.Configurations
{
    public sealed class UserPermissionOverrideConfiguration : IEntityTypeConfiguration<UserPermissionOverride>
    {
        public void Configure(EntityTypeBuilder<UserPermissionOverride> builder)
        {
            builder.ToTable("UserPermissionOverrides");

            builder.HasKey(x => new { x.UserId, x.PermissionKey });

            builder.Property(x => x.PermissionKey)
               .HasMaxLength(200)
               .IsRequired();

            builder.Property(x => x.Effect)
               .IsRequired();

            builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Permission>()
               .WithMany()
               .HasForeignKey(x => x.PermissionKey)
               .HasPrincipalKey(x => x.Key)
               .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.PermissionKey);
            builder.HasIndex(x => x.Effect);
        }
    }
}
