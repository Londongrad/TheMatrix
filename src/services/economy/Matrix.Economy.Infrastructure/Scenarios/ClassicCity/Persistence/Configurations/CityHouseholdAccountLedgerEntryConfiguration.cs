using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Configurations
{
    public sealed class
        CityHouseholdAccountLedgerEntryConfiguration : IEntityTypeConfiguration<CityHouseholdAccountLedgerEntry>
    {
        public void Configure(EntityTypeBuilder<CityHouseholdAccountLedgerEntry> builder)
        {
            builder.ToTable("City_Household_Account_Ledger_Entry");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasColumnName("id");
            builder.Property(x => x.HouseholdAccountId)
               .HasColumnName("household_account_id");
            builder.Property(x => x.CityId)
               .HasColumnName("city_id");
            builder.Property(x => x.OccurredAtUtc)
               .HasColumnName("occurred_at_utc");
            builder.Property(x => x.Kind)
               .HasConversion<string>()
               .HasColumnName("kind");
            builder.Property(x => x.Amount)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("amount");
            builder.Property(x => x.Title)
               .HasColumnName("title");
            builder.Property(x => x.Description)
               .HasColumnName("description");
            builder.Property(x => x.Source)
               .HasConversion<string>()
               .HasColumnName("source");
            builder.Property(x => x.ReferenceCode)
               .HasColumnName("reference_code");

            builder.HasIndex(x => x.CityId);
            builder.HasIndex(x => new
            {
                x.HouseholdAccountId,
                x.OccurredAtUtc,
                x.Id
            });
            builder.HasIndex(x => new
            {
                x.HouseholdAccountId,
                x.Kind,
                x.ReferenceCode
            })
               .IsUnique()
               .HasFilter("\"reference_code\" IS NOT NULL");
        }
    }
}
