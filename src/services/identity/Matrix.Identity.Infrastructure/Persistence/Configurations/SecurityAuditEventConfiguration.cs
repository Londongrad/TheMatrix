using Matrix.Identity.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Identity.Infrastructure.Persistence.Configurations
{
    internal sealed class SecurityAuditEventConfiguration : IEntityTypeConfiguration<SecurityAuditEventRecord>
    {
        public void Configure(EntityTypeBuilder<SecurityAuditEventRecord> builder)
        {
            builder.ToTable("SecurityAuditEvents");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.EventType)
               .HasConversion<int>()
               .IsRequired();

            builder.Property(x => x.IsSuccessful)
               .IsRequired();

            builder.Property(x => x.Subject)
               .HasMaxLength(256);

            builder.Property(x => x.IpAddress)
               .HasMaxLength(64);

            builder.Property(x => x.UserAgent)
               .HasMaxLength(512);

            builder.Property(x => x.DeviceId)
               .HasMaxLength(128);

            builder.Property(x => x.DeviceName)
               .HasMaxLength(256);

            builder.Property(x => x.Details)
               .HasMaxLength(512);

            builder.Property(x => x.OccurredAtUtc)
               .IsRequired();

            builder.HasIndex(x => new
            {
                x.EventType,
                x.OccurredAtUtc
            });
            builder.HasIndex(x => new
            {
                x.EventType,
                x.Subject,
                x.OccurredAtUtc
            });
            builder.HasIndex(x => new
            {
                x.EventType,
                x.IpAddress,
                x.OccurredAtUtc
            });
            builder.HasIndex(x => new
            {
                x.EventType,
                x.IsSuccessful,
                x.OccurredAtUtc
            });
            builder.HasIndex(x => new
            {
                x.UserId,
                x.OccurredAtUtc,
                x.Id
            });
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.SessionId);
        }
    }
}
