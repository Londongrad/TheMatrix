using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Common;
using Matrix.CityCore.Domain.Errors;
using Matrix.CityCore.Domain.Events.Cities;

namespace Matrix.CityCore.Domain.Cities
{
    /// <summary>
    ///     City aggregate root. Owns the city's lifecycle and metadata.
    ///     Simulation/time is owned by a separate aggregate (SimulationClock) linked by CityId.
    /// </summary>
    public sealed class City : AggregateRoot<CityId>
    {
        private City(
            CityId id,
            CityName name,
            CityStatus status,
            DateTimeOffset createdAtUtc,
            DateTimeOffset? archivedAtUtc)
            : base(id)
        {
            EnsureUtc(createdAtUtc);

            Name = name;
            Status = status;
            CreatedAtUtc = createdAtUtc;
            ArchivedAtUtc = archivedAtUtc;
        }

        private City()
            : base(default(CityId)) { }

        public CityName Name { get; private set; }
        public CityStatus Status { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; }
        public DateTimeOffset? ArchivedAtUtc { get; private set; }

        public bool IsArchived => Status == CityStatus.Archived;

        public static City Create(
            CityName name,
            DateTimeOffset createdAtUtc)
        {
            EnsureUtc(createdAtUtc);

            var city = new City(
                id: CityId.New(),
                name: name,
                status: CityStatus.Active,
                createdAtUtc: createdAtUtc,
                archivedAtUtc: null);

            city.AddDomainEvent(
                new CityCreatedDomainEvent(
                    CityId: city.Id,
                    Name: city.Name,
                    CreatedAtUtc: city.CreatedAtUtc));

            return city;
        }

        public void Rename(CityName newName)
        {
            GuardHelper.Ensure(
                condition: !IsArchived,
                value: Status,
                errorFactory: DomainErrorsFactory.CityIsArchived);

            if (newName.Equals(Name))
                return;

            CityName from = Name;
            Name = newName;

            AddDomainEvent(
                new CityRenamedDomainEvent(
                    CityId: Id,
                    From: from,
                    To: newName));
        }

        public void Archive(DateTimeOffset archivedAtUtc)
        {
            EnsureUtc(archivedAtUtc);

            if (IsArchived)
                return;

            Status = CityStatus.Archived;
            ArchivedAtUtc = archivedAtUtc;

            AddDomainEvent(
                new CityArchivedDomainEvent(
                    CityId: Id,
                    ArchivedAtUtc: archivedAtUtc));
        }

        private static void EnsureUtc(DateTimeOffset value)
        {
            GuardHelper.Ensure(
                condition: value.Offset == TimeSpan.Zero,
                value: value,
                errorFactory: DomainErrorsFactory.CityTimestampMustBeUtc);
        }
    }
}
