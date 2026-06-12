namespace Matrix.Resources.Infrastructure.Scenarios.ClassicCity.Persistence.Models
{
    public sealed class CityResourceDeletionState
    {
        private CityResourceDeletionState() { }

        public CityResourceDeletionState(
            Guid cityId,
            DateTimeOffset deletedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            CityId = cityId;
            DeletedAtUtc = deletedAtUtc;
            UpdatedAtUtc = updatedAtUtc;
        }

        public Guid CityId { get; private set; }
        public DateTimeOffset DeletedAtUtc { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public void Record(
            DateTimeOffset deletedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            if (deletedAtUtc < DeletedAtUtc)
                return;

            DeletedAtUtc = deletedAtUtc;
            UpdatedAtUtc = updatedAtUtc;
        }
    }
}
