using Matrix.CityCore.Domain.Cities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.CityCore.Infrastructure.Persistence.Configurations
{
    public sealed class CityConfiguration : IEntityTypeConfiguration<City>
    {
        public void Configure(EntityTypeBuilder<City> builder)
        {
            builder.ToTable("Cities");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new CityId(x))
               .ValueGeneratedNever();

            builder.Property(x => x.Name)
               .HasConversion(
                    convertToProviderExpression: x => x.Value,
                    convertFromProviderExpression: x => new CityName(x))
               .HasMaxLength(CityName.MaxLength)
               .IsRequired();

            builder.Property(x => x.Status)
               .HasConversion<int>()
               .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
               .IsRequired();

            builder.Property(x => x.ArchivedAtUtc)
               .IsRequired(false);

            builder.Ignore(x => x.DomainEvents);

            // Optimizations for common queries
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CreatedAtUtc);

            // Postgres optimistic concurrency
            builder.Property<uint>("xmin")
               .HasColumnName("xmin")
               .ValueGeneratedOnAddOrUpdate()
               .IsConcurrencyToken();
        }
    }
}
