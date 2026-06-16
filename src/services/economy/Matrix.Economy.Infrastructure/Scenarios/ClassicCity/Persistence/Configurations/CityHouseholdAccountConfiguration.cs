using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Configurations
{
    public sealed class CityHouseholdAccountConfiguration : IEntityTypeConfiguration<CityHouseholdAccount>
    {
        public void Configure(EntityTypeBuilder<CityHouseholdAccount> builder)
        {
            builder.ToTable("City_Household_Account");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasColumnName("id");
            builder.Property(x => x.CityId)
               .HasColumnName("city_id");
            builder.Property(x => x.Name)
               .HasColumnName("name");
            builder.Property(x => x.ExternalReferenceCode)
               .HasColumnName("external_reference_code");
            builder.Property(x => x.CreatedAtUtc)
               .HasColumnName("created_at_utc");
            builder.Property(x => x.UnitKind)
               .HasConversion<string>()
               .HasColumnName("unit_kind");
            builder.Property(x => x.UnitCode)
               .HasColumnName("unit_code");
            builder.Property(x => x.UnitDisplayName)
               .HasColumnName("unit_display_name");
            builder.Property(x => x.UnitSymbol)
               .HasColumnName("unit_symbol");

            builder.Property(x => x.Balance)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("balance_amount");

            builder.Property(x => x.TotalOpeningBalance)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("total_opening_balance_amount");

            builder.Property(x => x.TotalPayrollIncome)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("total_payroll_income_amount");

            builder.Property(x => x.TotalConsumerSpending)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("total_consumer_spending_amount");

            builder.HasIndex(x => x.CityId);
            builder.HasIndex(x => new
            {
                x.CityId,
                x.Name
            })
               .IsUnique();
            builder.HasIndex(x => x.ExternalReferenceCode);
            builder.HasIndex(x => new
            {
                x.CityId,
                x.ExternalReferenceCode
            })
               .IsUnique()
               .HasFilter("\"external_reference_code\" IS NOT NULL");
        }
    }
}
