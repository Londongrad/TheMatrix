using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Identity.Infrastructure.Persistence.Configurations
{
    public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("Permissions");

            builder.HasKey(x => x.Key);

            builder.Property(x => x.Key)
               .HasMaxLength(200)
               .IsRequired();

            builder.Property(x => x.Service)
               .HasMaxLength(100)
               .IsRequired();

            builder.Property(x => x.Group)
               .HasMaxLength(100)
               .IsRequired();

            builder.Property(x => x.Description)
               .HasMaxLength(400)
               .IsRequired();

            builder.Property(x => x.IsDeprecated)
               .IsRequired();

            builder.HasIndex(x => x.Service);
            builder.HasIndex(x => x.Group);
            builder.HasIndex(x => x.IsDeprecated);
        }
    }
}
