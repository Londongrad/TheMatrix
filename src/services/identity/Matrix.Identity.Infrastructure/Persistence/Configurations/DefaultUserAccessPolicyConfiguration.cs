using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Identity.Infrastructure.Persistence.Configurations
{
    public sealed class DefaultUserAccessPolicyConfiguration : IEntityTypeConfiguration<DefaultUserAccessPolicy>
    {
        public void Configure(EntityTypeBuilder<DefaultUserAccessPolicy> builder)
        {
            builder.ToTable("DefaultUserAccessPolicies");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Version)
               .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
               .IsRequired();

            builder.Property(x => x.UpdatedAtUtc)
               .IsRequired();
        }
    }
}
