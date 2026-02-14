using Matrix.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Identity.Infrastructure.Persistence.Configurations
{
    public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
    {
        public void Configure(EntityTypeBuilder<UserSession> builder)
        {
            builder.ToTable("UserSessions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .ValueGeneratedNever();

            builder.Property(x => x.UserId)
               .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
               .IsRequired();

            builder.Property(x => x.LastUsedAtUtc);

            builder.Property(x => x.RefreshTokenExpiresAtUtc)
               .IsRequired();

            builder.Property(x => x.IsPersistent)
               .IsRequired();

            builder.Property(x => x.IsRevoked)
               .IsRequired();

            builder.Property(x => x.RevokedAtUtc)
               .IsRequired(false);

            builder.Property(x => x.RevokedReason)
               .HasConversion<string?>()
               .IsRequired(false);

            builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(x => x.UserId)
               .OnDelete(DeleteBehavior.Cascade);

            builder.OwnsOne(
                navigationExpression: x => x.DeviceInfo,
                buildAction: device =>
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

                    device.HasIndex(d => d.DeviceId);
                });

            builder.OwnsOne(
                navigationExpression: x => x.GeoLocation,
                buildAction: geo =>
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

            builder.HasIndex(x => new
            {
                x.UserId,
                x.IsRevoked
            });

            builder.HasIndex(x => new
            {
                x.UserId,
                x.RefreshTokenExpiresAtUtc
            });
        }
    }
}
