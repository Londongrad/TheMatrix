using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Economy.Infrastructure.Persistence.Configurations
{
    public sealed class CityBusinessConfiguration : IEntityTypeConfiguration<CityBusiness>
    {
        public void Configure(EntityTypeBuilder<CityBusiness> builder)
        {
            builder.ToTable("City_Business");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.CityId).HasColumnName("city_id");
            builder.Property(x => x.Name).HasColumnName("name");
            builder.Property(x => x.TemplateKey).HasColumnName("template_key");
            builder.Property(x => x.Kind).HasConversion<string>().HasColumnName("kind");
            builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            builder.Property(x => x.UnitKind).HasConversion<string>().HasColumnName("unit_kind");
            builder.Property(x => x.UnitCode).HasColumnName("unit_code");
            builder.Property(x => x.UnitDisplayName).HasColumnName("unit_display_name");
            builder.Property(x => x.UnitSymbol).HasColumnName("unit_symbol");

            builder.Property(x => x.Balance)
                .HasConversion(m => m.Amount, v => new Money(v))
                .HasColumnName("balance_amount");

            builder.Property(x => x.TaxReserve)
                .HasConversion(m => m.Amount, v => new Money(v))
                .HasColumnName("tax_reserve_amount");

            builder.Property(x => x.TotalCapitalInjections)
                .HasConversion(m => m.Amount, v => new Money(v))
                .HasColumnName("total_capital_injections_amount");

            builder.Property(x => x.TotalRetailTurnover)
                .HasConversion(m => m.Amount, v => new Money(v))
                .HasColumnName("total_retail_turnover_amount");

            builder.Property(x => x.TotalNetSalesRevenue)
                .HasConversion(m => m.Amount, v => new Money(v))
                .HasColumnName("total_net_sales_revenue_amount");

            builder.Property(x => x.TotalOperatingExpenses)
                .HasConversion(m => m.Amount, v => new Money(v))
                .HasColumnName("total_operating_expenses_amount");

            builder.Property(x => x.TotalTaxRemitted)
                .HasConversion(m => m.Amount, v => new Money(v))
                .HasColumnName("total_tax_remitted_amount");

            builder.HasIndex(x => new { x.CityId, x.Name }).IsUnique();
            builder.HasIndex(x => new { x.CityId, x.TemplateKey })
                .IsUnique()
                .HasFilter("\"template_key\" IS NOT NULL");
            builder.HasIndex(x => x.CityId);
        }
    }
}
