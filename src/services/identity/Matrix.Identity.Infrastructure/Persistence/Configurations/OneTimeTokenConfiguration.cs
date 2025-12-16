using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Identity.Infrastructure.Persistence.Configurations
{
    internal sealed class OneTimeTokenConfiguration : IEntityTypeConfiguration<OneTimeToken>
    {
        public void Configure(EntityTypeBuilder<OneTimeToken> builder)
        {
            builder.ToTable("one_time_tokens");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.Purpose)
                .IsRequired();

            builder.Property(x => x.TokenHash)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();
            builder.Property(x => x.ExpiresAtUtc)
                .IsRequired();

            builder.Property(x => x.UsedAtUtc);
            builder.Property(x => x.RevokedAtUtc);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
