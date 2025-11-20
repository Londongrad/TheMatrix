using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations
{
    public class PersonConfiguration : IEntityTypeConfiguration<Person>
    {
        public void Configure(EntityTypeBuilder<Person> builder)
        {
            builder.ToTable("Persons");

            // PK
            builder.HasKey(p => p.Id);

            // ----- Value Object Ids -----
            builder.Property(p => p.Id)
                .HasConversion(
                    id => id.Value,                // PersonId -> Guid
                    value => PersonId.From(value)); // Guid -> PersonId

            builder.Property(p => p.HouseholdId)
                .HasConversion(
                    id => id.Value,
                    value => HouseholdId.From(value));

            builder.Property(p => p.DistrictId)
                .HasConversion(
                    id => id.Value,
                    value => DistrictId.From(value));

            // ----- Простые свойства / enum’ы -----
            builder.Property(p => p.LifeStatus)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(p => p.Sex)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(p => p.MaritalStatus)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(p => p.EducationLevel)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(p => p.EmploymentStatus)
                .HasConversion<string>()
                .IsRequired();

            // ----- LifeSpan (value object с BirthDate / DeathDate) -----
            builder.OwnsOne(p => p.LifeSpan, life =>
            {
                // BirthDate (NOT NULL)
                life.Property(l => l.BirthDate)
                    .HasConversion(
                        d => d.ToDateTime(TimeOnly.MinValue),      // DateOnly -> DateTime
                        dt => DateOnly.FromDateTime(dt))          // DateTime -> DateOnly
                    .HasColumnName("BirthDate")
                    .HasColumnType("date")
                    .IsRequired();

                // DeathDate (NULLABLE)
                life.Property(l => l.DeathDate)
                    .HasConversion(
                        d => d.HasValue
                            ? d.Value.ToDateTime(TimeOnly.MinValue)
                            : (DateTime?)null,
                        dt => dt.HasValue
                            ? DateOnly.FromDateTime(dt.Value)
                            : (DateOnly?)null)
                    .HasColumnName("DeathDate")
                    .HasColumnType("date");
            });

            // Computed accessors на агрегате игнорируем,
            // чтобы EF не пытался их мапить как отдельные колонки.
            builder.Ignore(p => p.BirthDate);
            builder.Ignore(p => p.DeathDate);
            builder.Ignore(p => p.IsAlive);


            // ----- Name (value object) -----

            builder.OwnsOne(p => p.Name, name =>
            {
                name.Property(n => n.FirstName)
                    .HasColumnName("FirstName")
                    .IsRequired();

                name.Property(n => n.LastName)
                    .HasColumnName("LastName")
                    .IsRequired();

                name.Property(n => n.Patronymic)
                    .HasColumnName("MiddleName");
            });

            // ----- Happiness (value object) -----

            builder.Property(p => p.Happiness)
                .HasConversion(
                    h => h.Value,
                    v => HappinessLevel.From(v))
                .HasColumnName("Happiness")
                .IsRequired();

            // ----- Health (value object) -----

            builder.Property(p => p.Health)
                .HasConversion(
                    h => h.Value,
                    v => HealthLevel.From(v))
                .HasColumnName("Health")
                .IsRequired();

            // ----- Weight (value object) -----

            builder.Property(p => p.Weight)
                .HasConversion(
                    w => w.Kilograms,        // из VO в decimal
                    kg => new BodyWeight(kg) // из decimal обратно в VO
                )
                .HasColumnName("WeightKg")
                .HasColumnType("decimal(5,2)");

            // ----- Personality (complex VO) -----

            builder.OwnsOne(p => p.Personality, pers =>
            {
                pers.Property(x => x.Optimism)
                    .HasColumnName("Optimism")
                    .IsRequired();

                pers.Property(x => x.Discipline)
                    .HasColumnName("Discipline")
                    .IsRequired();

                pers.Property(x => x.RiskTolerance)
                    .HasColumnName("RiskTolerance")
                    .IsRequired();

                pers.Property(x => x.Sociability)
                    .HasColumnName("Sociability")
                    .IsRequired();
            });

            // ----- Job (owned, nullable) -----

            builder.OwnsOne(p => p.Job, job =>
            {
                // WorkplaceId — value object
                job.Property(j => j.WorkplaceId)
                    .HasConversion(
                        id => id.Value,            // WorkplaceId -> Guid
                        value => WorkplaceId.From(value)) // Guid -> WorkplaceId
                    .HasColumnName("WorkplaceId");

                job.Property(j => j.Title)
                    .HasColumnName("JobTitle")
                    .HasMaxLength(200);
            });
        }

    }
}
