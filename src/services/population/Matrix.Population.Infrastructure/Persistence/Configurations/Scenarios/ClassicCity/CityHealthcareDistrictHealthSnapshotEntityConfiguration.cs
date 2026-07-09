using Matrix.Population.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations.Scenarios.ClassicCity;

public sealed class CityHealthcareDistrictHealthSnapshotEntityConfiguration
    : IEntityTypeConfiguration<CityHealthcareDistrictHealthSnapshotEntity>
{
    public void Configure(EntityTypeBuilder<CityHealthcareDistrictHealthSnapshotEntity> builder)
    {
        builder.ToTable("CityHealthcareDistrictHealthSnapshots");
        builder.HasKey(snapshot => new
        {
            snapshot.CityId,
            snapshot.DistrictId
        });
        builder.HasIndex(snapshot => snapshot.DistrictId);
    }
}
