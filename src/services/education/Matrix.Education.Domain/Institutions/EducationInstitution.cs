using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Common;
using Matrix.Education.Domain.Simulation;

namespace Matrix.Education.Domain.Institutions
{
    public sealed class EducationInstitution : AggregateRoot<EducationInstitutionId>
    {
        public const int MaxNameLength = 200;

        private EducationInstitution(
            EducationInstitutionId id,
            SimulationHostId simulationHostId,
            string name,
            EducationInstitutionKindKey kind,
            LocationAnchorId? locationAnchorId,
            int capacity,
            int currentEnrollmentCount,
            bool isActive)
            : base(id)
        {
            SimulationHostId = simulationHostId;
            Name = EnsureName(name);
            Kind = kind;
            LocationAnchorId = locationAnchorId;
            Capacity = EnsureCapacity(capacity);
            CurrentEnrollmentCount = EnsureEnrollmentCount(
                currentEnrollmentCount,
                Capacity);
            IsActive = isActive;
        }

        private EducationInstitution()
            : base(default(EducationInstitutionId))
        {
        }

        public EducationInstitutionId EducationInstitutionId => Id;
        public SimulationHostId SimulationHostId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public EducationInstitutionKindKey Kind { get; private set; }
        public LocationAnchorId? LocationAnchorId { get; private set; }
        public int Capacity { get; private set; }
        public int CurrentEnrollmentCount { get; private set; }
        public int AvailableSeatCount => Capacity - CurrentEnrollmentCount;
        public bool IsActive { get; private set; }

        public static EducationInstitution Create(
            EducationInstitutionId id,
            SimulationHostId simulationHostId,
            string name,
            EducationInstitutionKindKey kind,
            int capacity,
            LocationAnchorId? locationAnchorId = null)
        {
            return new EducationInstitution(
                id: id,
                simulationHostId: simulationHostId,
                name: name,
                kind: kind,
                locationAnchorId: locationAnchorId,
                capacity: capacity,
                currentEnrollmentCount: 0,
                isActive: true);
        }

        public void Rename(string name)
        {
            Name = EnsureName(name);
        }

        public void ChangeKind(EducationInstitutionKindKey kind)
        {
            Kind = kind;
        }

        public void BindLocation(LocationAnchorId locationAnchorId)
        {
            LocationAnchorId = locationAnchorId;
        }

        public void ClearLocation()
        {
            LocationAnchorId = null;
        }

        public void ChangeCapacity(int capacity)
        {
            int validatedCapacity = EnsureCapacity(capacity);

            if (validatedCapacity < CurrentEnrollmentCount)
                throw new InvalidOperationException(
                    "Institution capacity cannot be lower than its current enrollment count.");

            Capacity = validatedCapacity;
        }

        public bool TryReserveSeats(int count)
        {
            int validatedCount = EnsurePositiveSeatCount(count);

            if (!IsActive || validatedCount > AvailableSeatCount)
                return false;

            CurrentEnrollmentCount += validatedCount;
            return true;
        }

        public void ReleaseSeats(int count)
        {
            int validatedCount = EnsurePositiveSeatCount(count);

            if (validatedCount > CurrentEnrollmentCount)
                throw new InvalidOperationException(
                    "Cannot release more institution seats than are currently reserved.");

            CurrentEnrollmentCount -= validatedCount;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        private static string EnsureName(string? value)
        {
            string normalized = GuardHelper.AgainstNullOrWhiteSpace(
                    value: value,
                    propertyName: nameof(Name))
               .Trim();

            return normalized.Length <= MaxNameLength
                ? normalized
                : throw new ArgumentOutOfRangeException(
                    paramName: nameof(value),
                    message: $"Institution names cannot exceed {MaxNameLength} characters.");
        }

        private static int EnsureCapacity(int value)
        {
            return value > 0
                ? value
                : throw new ArgumentOutOfRangeException(
                    paramName: nameof(value),
                    message: "Institution capacity must be positive.");
        }

        private static int EnsureEnrollmentCount(int value, int capacity)
        {
            return value >= 0 && value <= capacity
                ? value
                : throw new ArgumentOutOfRangeException(
                    paramName: nameof(value),
                    message: "Current enrollment count must fit institution capacity.");
        }

        private static int EnsurePositiveSeatCount(int value)
        {
            return value > 0
                ? value
                : throw new ArgumentOutOfRangeException(
                    paramName: nameof(value),
                    message: "Seat counts must be positive.");
        }
    }
}
