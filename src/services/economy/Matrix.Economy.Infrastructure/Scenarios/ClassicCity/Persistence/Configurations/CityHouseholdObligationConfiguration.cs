using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Configurations
{
    public sealed class CityHouseholdObligationConfiguration : IEntityTypeConfiguration<CityHouseholdObligation>
    {
        public void Configure(EntityTypeBuilder<CityHouseholdObligation> builder)
        {
            builder.ToTable("City_Household_Obligation");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasColumnName("id");
            builder.Property(x => x.CityId)
               .HasColumnName("city_id");
            builder.Property(x => x.HouseholdAccountId)
               .HasColumnName("household_account_id");
            builder.Property(x => x.ProviderBusinessId)
               .HasColumnName("provider_business_id");
            builder.Property(x => x.Name)
               .HasColumnName("name");
            builder.Property(x => x.Kind)
               .HasConversion<string>()
               .HasColumnName("kind");
            builder.Property(x => x.BillingCadence)
               .HasConversion<string>()
               .HasColumnName("billing_cadence");
            builder.Property(x => x.CreatedAtUtc)
               .HasColumnName("created_at_utc");
            builder.Property(x => x.IsActive)
               .HasColumnName("is_active");
            builder.Property(x => x.UnitKind)
               .HasConversion<string>()
               .HasColumnName("unit_kind");
            builder.Property(x => x.UnitCode)
               .HasColumnName("unit_code");
            builder.Property(x => x.UnitDisplayName)
               .HasColumnName("unit_display_name");
            builder.Property(x => x.UnitSymbol)
               .HasColumnName("unit_symbol");
            builder.Property(x => x.BaseChargeAmount)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("base_charge_amount");
            builder.Property(x => x.BaseTaxAmount)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("base_tax_amount");

            builder.Property(x => x.ChargeAmount)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("charge_amount");

            builder.Property(x => x.TaxAmount)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("tax_amount");

            builder.Property(x => x.NextChargeDueAtUtc)
               .HasColumnName("next_charge_due_at_utc");
            builder.Property(x => x.LastChargedAtUtc)
               .HasColumnName("last_charged_at_utc");
            builder.Property(x => x.LastChargeAttemptedAtUtc)
               .HasColumnName("last_charge_attempted_at_utc");
            builder.Property(x => x.FirstMissedChargeDueAtUtc)
               .HasColumnName("first_missed_charge_due_at_utc");
            builder.Property(x => x.ServiceCutoffAtUtc)
               .HasColumnName("service_cutoff_at_utc");
            builder.Property(x => x.EvictionNoticeIssuedAtUtc)
               .HasColumnName("eviction_notice_issued_at_utc");
            builder.Property(x => x.EvictionEligibleAtUtc)
               .HasColumnName("eviction_eligible_at_utc");
            builder.Property(x => x.ChargeCount)
               .HasColumnName("charge_count");
            builder.Property(x => x.MissedChargeCount)
               .HasColumnName("missed_charge_count");

            builder.HasIndex(x => x.CityId);
            builder.HasIndex(x => x.HouseholdAccountId);
            builder.HasIndex(x => x.ProviderBusinessId);
            builder.HasIndex(x => new
            {
                x.CityId,
                x.NextChargeDueAtUtc
            });
            builder.HasIndex(x => new
            {
                x.HouseholdAccountId,
                x.ProviderBusinessId,
                x.Name
            })
               .IsUnique();
        }
    }
}
