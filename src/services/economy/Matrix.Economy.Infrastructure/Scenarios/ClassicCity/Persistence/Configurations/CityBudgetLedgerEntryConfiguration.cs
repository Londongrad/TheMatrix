using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Configurations
{
    public sealed class CityBudgetLedgerEntryConfiguration : IEntityTypeConfiguration<CityBudgetLedgerEntry>
    {
        public void Configure(EntityTypeBuilder<CityBudgetLedgerEntry> builder)
        {
            builder.ToTable("City_Budget_Ledger_Entries");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasColumnName("id");
            builder.Property(x => x.CityId)
               .HasColumnName("city_id");
            builder.Property(x => x.OccurredAtUtc)
               .HasColumnName("occurred_at_utc");
            builder.Property(x => x.Kind)
               .HasConversion<string>()
               .HasColumnName("kind");
            builder.Property(x => x.Category)
               .HasConversion<string>()
               .HasColumnName("category");
            builder.Property(x => x.Title)
               .HasColumnName("title");
            builder.Property(x => x.Description)
               .HasColumnName("description");
            builder.Property(x => x.Source)
               .HasConversion<string>()
               .HasColumnName("source");
            builder.Property(x => x.ReferenceCode)
               .HasColumnName("reference_code");
            builder.Property(x => x.Amount)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("amount");

            builder.HasIndex(x => new
            {
                x.CityId,
                x.OccurredAtUtc,
                x.Id
            });
            builder.HasIndex(x => x.ReferenceCode);
        }
    }
}
