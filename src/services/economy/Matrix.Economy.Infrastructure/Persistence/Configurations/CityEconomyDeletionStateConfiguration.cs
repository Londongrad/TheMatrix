using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Economy.Infrastructure.Persistence.Configurations
{
    public sealed class CityEconomyDeletionStateConfiguration : IEntityTypeConfiguration<CityEconomyDeletionState>
    {
        public void Configure(EntityTypeBuilder<CityEconomyDeletionState> builder)
        {
            builder.ToTable("CityEconomyDeletionStates");
            builder.HasKey(x => x.CityId);
            builder.Property(x => x.DeletedAtUtc)
               .IsRequired();
            builder.Property(x => x.UpdatedAtUtc)
               .IsRequired();
            builder.HasIndex(x => x.DeletedAtUtc);
        }
    }
}
