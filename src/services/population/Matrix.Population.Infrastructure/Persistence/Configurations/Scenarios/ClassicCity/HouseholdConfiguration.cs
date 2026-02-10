using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations.Scenarios.ClassicCity
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

            builder.Property(x => x.CityId)
               .HasConversion(
                    convertToProviderExpression: id => id.HasValue
                        ? id.Value.Value
                        : (Guid?)null,
                    convertFromProviderExpression: value => value.HasValue
                        ? CityId.From(value.Value)
                        : null);

            builder.Property(x => x.DistrictId)
               .HasConversion(
                    convertToProviderExpression: id => id.HasValue
                        ? id.Value.Value
                        : (Guid?)null,
                    convertFromProviderExpression: value => value.HasValue
                        ? DistrictId.From(value.Value)
                        : null);

            builder.Property(x => x.ResidentialBuildingId)
               .HasConversion(
                    convertToProviderExpression: id => id.HasValue
                        ? id.Value.Value
                        : (Guid?)null,
                    convertFromProviderExpression: value => value.HasValue
                        ? ResidentialBuildingId.From(value.Value)
                        : null);

            builder.Property(x => x.HousingStatus)
               .HasConversion<string>()
               .HasMaxLength(32)
               .IsRequired();

            builder.Property(x => x.Size)
               .HasConversion(
                    convertToProviderExpression: size => size.Value,
                    convertFromProviderExpression: value => HouseholdSize.From(value))
               .HasColumnName("Size")
               .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
               .IsRequired();

            builder.HasIndex(x => x.CityId);
            builder.HasIndex(x => x.DistrictId);
            builder.HasIndex(x => x.ResidentialBuildingId);
        }
    }
}
