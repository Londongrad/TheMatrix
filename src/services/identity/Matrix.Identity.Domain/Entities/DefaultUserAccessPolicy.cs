namespace Matrix.Identity.Domain.Entities
{
    public sealed class DefaultUserAccessPolicy
    {
        public static readonly Guid SingletonId = Guid.Parse("D9E1B4D4-5DF6-4AD6-9B5B-A0E8B5A4E0C1");

        private DefaultUserAccessPolicy() { }

        private DefaultUserAccessPolicy(
            Guid id,
            int version,
            DateTime createdAtUtc,
            DateTime updatedAtUtc)
        {
            Id = id;
            Version = version;
            CreatedAtUtc = createdAtUtc;
            UpdatedAtUtc = updatedAtUtc;
        }

        public Guid Id { get; private set; }
        public int Version { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }
        public DateTime UpdatedAtUtc { get; private set; }

        public static DefaultUserAccessPolicy CreateDefault(DateTime nowUtc)
        {
            return new DefaultUserAccessPolicy(
                id: SingletonId,
                version: 1,
                createdAtUtc: nowUtc,
                updatedAtUtc: nowUtc);
        }

        public void Touch(DateTime nowUtc)
        {
            Version++;
            UpdatedAtUtc = nowUtc;
        }
    }
}
