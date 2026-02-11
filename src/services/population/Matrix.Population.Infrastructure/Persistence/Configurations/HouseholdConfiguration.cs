using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations
{
    public sealed class HouseholdConfiguration : IEntityTypeConfiguration<Household>
    {
        public void Configure(EntityTypeBuilder<Household> builder)
        {
            builder.ToTable("Households");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => HouseholdId.From(value));

            builder.Property(x => x.Size)
               .HasConversion(
                    convertToProviderExpression: size => size.Value,
                    convertFromProviderExpression: value => HouseholdSize.From(value))
               .HasColumnName("Size")
               .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
               .IsRequired();
        }
    }
}
