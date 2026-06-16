using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Configurations
{
    public class CityBudgetConfiguration : IEntityTypeConfiguration<CityBudget>
    {
        public void Configure(EntityTypeBuilder<CityBudget> builder)
        {
            builder.ToTable("City_Budget");

            builder.HasKey(b => b.Id);

            builder
               .Property(b => b.Id)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => new CityBudgetId(value))
               .HasColumnName("id");

            builder
               .Property(b => b.CityId)
               .HasColumnName("city_id");

            builder
               .Property(b => b.UnitKind)
               .HasConversion<string>()
               .HasColumnName("unit_kind");

            builder
               .Property(b => b.UnitCode)
               .HasColumnName("unit_code");

            builder
               .Property(b => b.UnitDisplayName)
               .HasColumnName("unit_display_name");

            builder
               .Property(b => b.UnitSymbol)
               .HasColumnName("unit_symbol");

            builder
               .Property(b => b.Balance)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("balance_amount");

            builder
               .Property(b => b.TotalTaxIncome)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("total_tax_income_amount");

            builder
               .Property(b => b.TotalIncomeTaxIncome)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("total_income_tax_income_amount");

            builder
               .Property(b => b.TotalSalesTaxIncome)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("total_sales_tax_income_amount");

            builder
               .Property(b => b.TotalDirectRevenue)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("total_direct_revenue_amount");

            builder
               .Property(b => b.TotalCityExpenses)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("total_city_expenses_amount");

            builder
               .Property(b => b.TotalRetailTurnover)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("total_retail_turnover_amount");

            builder
               .Property(b => b.TotalGrossPayroll)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("total_gross_payroll_amount");

            builder
               .Property(b => b.TotalNetPayroll)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("total_net_payroll_amount");

            builder.HasIndex(b => b.CityId)
               .IsUnique();
        }
    }
}
