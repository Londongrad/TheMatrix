using Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Persistence.Configurations
{
    public sealed class CityResourceDeletionStateConfiguration
        : IEntityTypeConfiguration<CityResourceDeletionState>
    {
        public void Configure(EntityTypeBuilder<CityResourceDeletionState> builder)
        {
            builder.ToTable("CityResourceDeletionStates");
            builder.HasKey(x => x.CityId);
            builder.Property(x => x.DeletedAtUtc)
               .IsRequired();
            builder.Property(x => x.UpdatedAtUtc)
               .IsRequired();
            builder.HasIndex(x => x.DeletedAtUtc);
        }
    }
}
