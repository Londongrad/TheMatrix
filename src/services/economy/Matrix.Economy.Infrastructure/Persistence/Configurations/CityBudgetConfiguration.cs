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

            // Money -> decimal
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
        }
    }
}
