using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Configurations
{
    public sealed class CityBudgetSettlementConfiguration : IEntityTypeConfiguration<CityBudgetSettlement>
    {
        public void Configure(EntityTypeBuilder<CityBudgetSettlement> builder)
        {
            builder.ToTable("City_Budget_Settlements");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasColumnName("id");
            builder.Property(x => x.CityId)
               .HasColumnName("city_id");
            builder.Property(x => x.TickId)
               .HasColumnName("tick_id");
            builder.Property(x => x.CurrentDate)
               .HasColumnName("current_date");
            builder.Property(x => x.SettledDays)
               .HasColumnName("settled_days");
            builder.Property(x => x.HouseholdCount)
               .HasColumnName("household_count");
            builder.Property(x => x.ResidentCount)
               .HasColumnName("resident_count");
            builder.Property(x => x.CorrelationId)
               .HasColumnName("correlation_id");
            builder.Property(x => x.OccurredAtUtc)
               .HasColumnName("occurred_at_utc");

            builder.Property(x => x.GrossPayroll)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("gross_payroll_amount");
            builder.Property(x => x.IncomeTax)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("income_tax_amount");
            builder.Property(x => x.NetPayroll)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("net_payroll_amount");
            builder.Property(x => x.RetailTurnover)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("retail_turnover_amount");
            builder.Property(x => x.RetailTax)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("retail_tax_amount");
            builder.Property(x => x.HousingSpend)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("housing_spend_amount");

            builder.HasIndex(x => new
            {
                x.CityId,
                x.TickId
            })
               .IsUnique();
            builder.HasIndex(x => x.CorrelationId)
               .IsUnique();
        }
    }
}
