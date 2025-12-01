using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Matrix.Population.Infrastructure.Persistence.Configurations
{
    public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
    {
        public void Configure(EntityTypeBuilder<Person> builder)
        {
            builder.ToTable("Persons");

            // ---------- PK ----------

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .HasConversion(
                    id => id.Value,              // PersonId -> Guid
                    value => PersonId.From(value)); // Guid -> PersonId

            // Если HouseholdId есть у Person – мапим его так.
            builder.Property(p => p.HouseholdId)
                .HasConversion(
                    id => id.Value,
                    value => HouseholdId.From(value))
                .IsRequired();

            // ---------- Простые enum’ы / свойства ----------

            builder.Property(p => p.Sex)
                .HasConversion<string>()
                .IsRequired();

            // Happiness (VO)
            builder.Property(p => p.Happiness)
                .HasConversion(
                    h => h.Value,
                    v => HappinessLevel.From(v))
                .HasColumnName("Happiness")
                .IsRequired();

            // Вес
            builder.Property(p => p.Weight)
                .HasConversion(
                    w => w.Kilograms,
                    kg => new BodyWeight(kg))
                .HasColumnName("WeightKg")
                .HasColumnType("decimal(5,2)");

            // ---------- Name (VO) ----------

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

            // ---------- Life (LifeState: Status + Span + Health) ----------

            builder.OwnsOne(p => p.Life, life =>
            {
                life.Property(l => l.Status)
                    .HasConversion<string>()
                    .HasColumnName("LifeStatus")
                    .IsRequired();

                life.Property(l => l.Health)
                    .HasConversion(
                        h => h.Value,
                        v => HealthLevel.From(v))
                    .HasColumnName("Health")
                    .IsRequired();

                life.OwnsOne(l => l.Span, span =>
                {
                    span.Property(s => s.BirthDate)
                        .HasConversion(
                            d => d.ToDateTime(TimeOnly.MinValue),
                            dt => DateOnly.FromDateTime(dt))
                        .HasColumnName("BirthDate")
                        .HasColumnType("date")
                        .IsRequired();

                    span.Property(s => s.DeathDate)
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
            });

            // Прокси свойства игнорируем
            builder.Ignore(p => p.IsAlive);
            builder.Ignore(p => p.LifeStatus);
            builder.Ignore(p => p.BirthDate);
            builder.Ignore(p => p.DeathDate);
            builder.Ignore(p => p.Health);

            builder.Ignore(p => p.MaritalStatus);
            builder.Ignore(p => p.SpouseId);

            builder.Ignore(p => p.EducationLevel);

            // ---------- Education (EducationInfo VO) ----------

            builder.OwnsOne(p => p.Education, edu =>
            {
                edu.Property(e => e.Level)
                    .HasConversion<string>()
                    .HasColumnName("EducationLevel")
                    .IsRequired();
            });

            // ---------- Marital (MaritalInfo VO) ----------

            builder.OwnsOne(p => p.Marital, marital =>
            {
                marital.Property(m => m.Status)
                    .HasConversion<string>()
                    .HasColumnName("MaritalStatus")
                    .IsRequired();

                // SpouseId: PersonId? -> Guid?
                marital.Property(m => m.SpouseId)
                    .HasConversion(
                        id => id.HasValue ? id.Value.Value : (Guid?)null,
                        value => value.HasValue ? PersonId.From(value.Value) : null)
                    .HasColumnName("SpouseId");
            });

            // ---------- Employment (EmploymentInfo VO + вложенный Job) ----------

            builder.OwnsOne(p => p.Employment, employment =>
            {
                employment.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasColumnName("EmploymentStatus")
                    .IsRequired();

                employment.OwnsOne(e => e.Job, job =>
                {
                    job.Property(j => j.WorkplaceId)
                        .HasConversion(
                            id => id.Value,
                            value => WorkplaceId.From(value))
                        .HasColumnName("WorkplaceId");

                    job.Property(j => j.Title)
                        .HasColumnName("JobTitle")
                        .HasMaxLength(200);
                });
            });

            // ---------- Personality (VO) ----------

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
        }
    }
}
