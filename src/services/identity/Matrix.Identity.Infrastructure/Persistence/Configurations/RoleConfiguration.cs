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
               .HasMaxLength(64)
               .IsRequired();

            builder.HasIndex(x => x.Name)
               .IsUnique();

            builder.Property(x => x.IsSystem)
               .IsRequired();
            builder.Property(x => x.CreatedAtUtc)
               .IsRequired();
        }
    }
}
