using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Identity.Infrastructure.Persistence.Configurations
{
    public sealed class DefaultUserAccessOverrideConfiguration : IEntityTypeConfiguration<DefaultUserAccessOverride>
    {
        public void Configure(EntityTypeBuilder<DefaultUserAccessOverride> builder)
        {
            builder.ToTable("DefaultUserAccessOverrides");

            builder.HasKey(x => new
            {
                x.PolicyId,
                x.PermissionKey
            });

            builder.Property(x => x.PermissionKey)
               .HasMaxLength(DefaultUserAccessOverride.PermissionKeyMaxLength)
               .IsRequired();

            builder.Property(x => x.Effect)
               .IsRequired();

            builder.HasOne<DefaultUserAccessPolicy>()
               .WithMany()
               .HasForeignKey(x => x.PolicyId)
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
