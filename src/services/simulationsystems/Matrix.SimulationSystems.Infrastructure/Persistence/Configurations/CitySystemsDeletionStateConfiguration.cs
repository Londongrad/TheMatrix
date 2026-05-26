using Matrix.SimulationSystems.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.SimulationSystems.Infrastructure.Persistence.Configurations
{
    public sealed class CitySystemsDeletionStateConfiguration : IEntityTypeConfiguration<CitySystemsDeletionState>
    {
        public void Configure(EntityTypeBuilder<CitySystemsDeletionState> builder)
        {
            builder.ToTable("CitySystemsDeletionStates");
            builder.HasKey(x => x.CityId);
            builder.Property(x => x.DeletedAtUtc)
               .IsRequired();
            builder.Property(x => x.UpdatedAtUtc)
               .IsRequired();
            builder.HasIndex(x => x.DeletedAtUtc);
        }
    }
}
