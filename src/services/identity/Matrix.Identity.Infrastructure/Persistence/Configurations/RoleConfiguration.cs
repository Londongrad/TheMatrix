using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Identity.Infrastructure.Persistence.Configurations
{
    public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Roles");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
               .HasMaxLength(Role.NameMaxLength)
               .IsRequired();

            builder.Property(x => x.NormalizedName)
               .HasMaxLength(Role.NameMaxLength)
               .IsRequired();

            builder.HasIndex(x => x.NormalizedName)
               .IsUnique()
               .HasDatabaseName("ux_roles_normalized_name");

            builder.Property(x => x.IsSystem)
               .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
               .IsRequired();
        }
    }
}
