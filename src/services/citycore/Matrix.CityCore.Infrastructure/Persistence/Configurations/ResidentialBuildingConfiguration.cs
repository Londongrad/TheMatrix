using Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Topology;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.CityCore.Infrastructure.Persistence.Configurations
{
    public sealed class ResidentialBuildingConfiguration : IEntityTypeConfiguration<ResidentialBuilding>
    {
        public void Configure(EntityTypeBuilder<ResidentialBuilding> builder)
        {
            builder.ToTable("ResidentialBuildings");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new ResidentialBuildingId(x))
               .ValueGeneratedNever();

            builder.Property(x => x.CityId)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new CityId(x))
               .IsRequired();

            builder.Property(x => x.DistrictId)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new DistrictId(x))
               .IsRequired();

            builder.Property(x => x.Name)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new ResidentialBuildingName(x))
               .HasMaxLength(ResidentialBuildingName.MaxLength)
               .IsRequired();

            builder.Property(x => x.Type)
               .HasConversion<int>()
               .IsRequired();

            builder.Property(x => x.ResidentCapacity)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => ResidentCapacity.From(x))
               .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
               .IsRequired();

            builder.Ignore(x => x.DomainEvents);

            builder.HasIndex(x => x.CityId);
            builder.HasIndex(x => x.DistrictId);

            builder
               .HasOne<City>()
               .WithMany()
               .HasForeignKey(x => x.CityId)
               .OnDelete(DeleteBehavior.Cascade);

            builder
               .HasOne<District>()
               .WithMany()
               .HasForeignKey(x => x.DistrictId)
               .OnDelete(DeleteBehavior.Cascade);

            builder.Property<uint>("xmin")
               .HasColumnName("xmin")
               .ValueGeneratedOnAddOrUpdate()
               .IsConcurrencyToken();
        }
    }
}
