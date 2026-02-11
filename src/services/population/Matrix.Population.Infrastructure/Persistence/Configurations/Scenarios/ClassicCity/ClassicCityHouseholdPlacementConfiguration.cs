using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations.Scenarios.ClassicCity
{
    public sealed class ClassicCityHouseholdPlacementConfiguration
        : IEntityTypeConfiguration<ClassicCityHouseholdPlacement>
    {
        public void Configure(EntityTypeBuilder<ClassicCityHouseholdPlacement> builder)
        {
            builder.ToTable("ClassicCityHouseholdPlacements");

            builder.HasKey(x => x.HouseholdId);

            builder.Property(x => x.HouseholdId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => HouseholdId.From(value));

            builder.Property(x => x.CityId)
               .HasConversion(
                    convertToProviderExpression: id => id.Value,
                    convertFromProviderExpression: value => CityId.From(value))
               .IsRequired();

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

            builder.HasOne<Household>()
               .WithMany()
               .HasForeignKey(x => x.HouseholdId)
               .HasPrincipalKey(x => x.Id)
               .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.CityId);
            builder.HasIndex(x => x.DistrictId);
            builder.HasIndex(x => x.ResidentialBuildingId);
        }
    }
}
