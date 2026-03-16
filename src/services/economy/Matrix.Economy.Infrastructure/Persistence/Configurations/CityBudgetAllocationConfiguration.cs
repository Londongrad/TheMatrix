using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Economy.Infrastructure.Persistence.Configurations
{
    public sealed class CityBudgetAllocationConfiguration : IEntityTypeConfiguration<CityBudgetAllocation>
    {
        public void Configure(EntityTypeBuilder<CityBudgetAllocation> builder)
        {
            builder.ToTable("City_Budget_Allocation");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasColumnName("id");
            builder.Property(x => x.CityId)
               .HasColumnName("city_id");
            builder.Property(x => x.Category)
               .HasConversion<string>()
               .HasColumnName("category");
            builder.Property(x => x.CreatedAtUtc)
               .HasColumnName("created_at_utc");
            builder.Property(x => x.UpdatedAtUtc)
               .HasColumnName("updated_at_utc");
            builder.Property(x => x.UnitKind)
               .HasConversion<string>()
               .HasColumnName("unit_kind");
            builder.Property(x => x.UnitCode)
               .HasColumnName("unit_code");
            builder.Property(x => x.UnitDisplayName)
               .HasColumnName("unit_display_name");
            builder.Property(x => x.UnitSymbol)
               .HasColumnName("unit_symbol");

            builder.Property(x => x.TargetAmount)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("target_amount");

            builder.Property(x => x.TotalSpent)
               .HasConversion(
                    convertToProviderExpression: m => m.Amount,
                    convertFromProviderExpression: v => new Money(v))
               .HasColumnName("total_spent_amount");

            builder.HasIndex(x => x.CityId);
            builder.HasIndex(x => new
                {
                    x.CityId,
                    x.Category
                })
               .IsUnique();
        }
    }
}
