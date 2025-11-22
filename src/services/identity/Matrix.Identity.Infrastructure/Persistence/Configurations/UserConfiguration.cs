using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Identity.Infrastructure.Persistence.Configurations
{
    public sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .ValueGeneratedNever();

            // Email (Value Object)
            builder.OwnsOne(u => u.Email, email =>
            {
                email.Property(e => e.Value)
                    .HasColumnName("Email")
                    .HasMaxLength(256)
                    .IsRequired();

                email.HasIndex(e => e.Value)
                    .IsUnique();
            });

            builder.Property(u => u.Username)
                .HasMaxLength(32)
                .IsRequired();

            builder.HasIndex(u => u.Username)
                .IsUnique();

            builder.Property(u => u.AvatarUrl)
                .HasMaxLength(512);

            builder.Property(u => u.PasswordHash)
                .IsRequired();

            builder.Property(u => u.CreatedAtUtc)
                .IsRequired();

            builder.Property(u => u.IsEmailConfirmed)
                .IsRequired();

            builder.Property(u => u.IsLocked)
                .IsRequired();

            builder.Navigation(u => u.RefreshTokens)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            // RefreshTokens как owned-коллекция
            builder.OwnsMany(u => u.RefreshTokens, token =>
            {
                token.ToTable("UserRefreshTokens");

                token.WithOwner().HasForeignKey("UserId");

                token.HasKey(t => t.Id);

                token.Property(t => t.Id)
                    .ValueGeneratedNever();

                token.Property(t => t.TokenHash)
                    .IsRequired();

                token.Property(t => t.CreatedAtUtc)
                    .IsRequired();

                token.Property(t => t.ExpiresAtUtc)
                    .IsRequired();

                token.Property(t => t.IsRevoked)
                    .IsRequired();
            });
        }
    }
}
