using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Economy.Infrastructure.Persistence.Configurations
{
    public sealed class CityBusinessLedgerEntryConfiguration : IEntityTypeConfiguration<CityBusinessLedgerEntry>
    {
        public void Configure(EntityTypeBuilder<CityBusinessLedgerEntry> builder)
        {
            builder.ToTable("City_Business_Ledger_Entry");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.BusinessId).HasColumnName("business_id");
            builder.Property(x => x.CityId).HasColumnName("city_id");
            builder.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc");
            builder.Property(x => x.Kind).HasConversion<string>().HasColumnName("kind");
            builder.Property(x => x.Amount)
                .HasConversion(m => m.Amount, v => new Money(v))
                .HasColumnName("amount");
            builder.Property(x => x.TaxAmount)
                .HasConversion(m => m.Amount, v => new Money(v))
                .HasColumnName("tax_amount");
            builder.Property(x => x.Title).HasColumnName("title");
            builder.Property(x => x.Description).HasColumnName("description");
            builder.Property(x => x.Source).HasConversion<string>().HasColumnName("source");
            builder.Property(x => x.ReferenceCode).HasColumnName("reference_code");

            builder.HasIndex(x => new { x.BusinessId, x.OccurredAtUtc });
            builder.HasIndex(x => x.CityId);
        }
    }
}
