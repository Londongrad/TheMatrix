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

            // Username (Value Object)
            builder.OwnsOne(u => u.Username, username =>
            {
                username.Property(x => x.Value)
                    .HasColumnName("Username")
                    .HasMaxLength(32)
                    .IsRequired();

                username.HasIndex(x => x.Value)
                    .IsUnique();
            });

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

            // RefreshTokens как owned-коллекция
            builder.OwnsMany(u => u.RefreshTokens, token =>
            {
                token.ToTable("UserRefreshTokens");

                token.WithOwner().HasForeignKey("UserId");

                token.HasKey(t => t.Id);

                token.Property(t => t.Id)
                    .ValueGeneratedNever();

                token.Property(t => t.TokenHash)
                    .HasMaxLength(512)
                    .IsRequired();

                token.Property(t => t.CreatedAtUtc)
                    .IsRequired();

                token.Property(t => t.ExpiresAtUtc)
                    .IsRequired();

                token.Property(t => t.IsRevoked)
                    .IsRequired();

                token.Property(t => t.LastUsedAtUtc);

                // Часто полезно иметь индекс по TokenHash (для поиска по хэшу)
                token.HasIndex(t => t.TokenHash);

                // DeviceInfo (Value Object внутри refresh-токена)
                token.OwnsOne(t => t.DeviceInfo, device =>
                {
                    device.Property(d => d.DeviceId)
                        .HasColumnName("DeviceId")
                        .HasMaxLength(128)
                        .IsRequired();

                    device.Property(d => d.DeviceName)
                        .HasColumnName("DeviceName")
                        .HasMaxLength(256)
                        .IsRequired();

                    device.Property(d => d.UserAgent)
                        .HasColumnName("UserAgent")
                        .HasMaxLength(512)
                        .IsRequired();

                    device.Property(d => d.IpAddress)
                        .HasColumnName("IpAddress")
                        .HasMaxLength(64);
                });

                // GeoLocation (Value Object внутри refresh-токена)
                token.OwnsOne(t => t.GeoLocation, geo =>
                {
                    geo.Property(g => g.Country)
                        .HasColumnName("Country")
                        .HasMaxLength(128);

                    geo.Property(g => g.Region)
                        .HasColumnName("Region")
                        .HasMaxLength(128);

                    geo.Property(g => g.City)
                        .HasColumnName("City")
                        .HasMaxLength(128);
                });
            });

            // Навигация на коллекцию через приватное поле _refreshTokens
            builder.Navigation(u => u.RefreshTokens)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
