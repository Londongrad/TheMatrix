using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Economy.Infrastructure.Persistence.Configurations
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
                    id => id.Value,
                    value => new CityBudgetId(value))
                .HasColumnName("id");

            builder
                .Property(b => b.CityId)
                .HasColumnName("city_id");

            builder
                .Property(b => b.Balance)
                .HasConversion(
                    m => m.Amount,
                    v => new Money(v))
                .HasColumnName("balance_amount");

            builder
                .Property(b => b.TotalTaxIncome)
                .HasConversion(
                    m => m.Amount,
                    v => new Money(v))
                .HasColumnName("total_tax_income_amount");

            builder
                .Property(b => b.TotalIncomeTaxIncome)
                .HasConversion(
                    m => m.Amount,
                    v => new Money(v))
                .HasColumnName("total_income_tax_income_amount");

            builder
                .Property(b => b.TotalSalesTaxIncome)
                .HasConversion(
                    m => m.Amount,
                    v => new Money(v))
                .HasColumnName("total_sales_tax_income_amount");

            builder
                .Property(b => b.TotalCityExpenses)
                .HasConversion(
                    m => m.Amount,
                    v => new Money(v))
                .HasColumnName("total_city_expenses_amount");

            builder
                .Property(b => b.TotalRetailTurnover)
                .HasConversion(
                    m => m.Amount,
                    v => new Money(v))
                .HasColumnName("total_retail_turnover_amount");

            builder
                .Property(b => b.TotalGrossPayroll)
                .HasConversion(
                    m => m.Amount,
                    v => new Money(v))
                .HasColumnName("total_gross_payroll_amount");

            builder
                .Property(b => b.TotalNetPayroll)
                .HasConversion(
                    m => m.Amount,
                    v => new Money(v))
                .HasColumnName("total_net_payroll_amount");

            builder.HasIndex(b => b.CityId).IsUnique();
        }
    }
}
