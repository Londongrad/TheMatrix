namespace Matrix.Identity.Domain.Entities
{
    public sealed class Permission
    {
        private Permission() { }

        public Permission(
            string key,
            string service,
            string group,
            string description)
        {
            Key = key;
            Service = service;
            Group = group;
            Description = description;
        }

        public string Key { get; private set; } = null!;
        public string Service { get; private set; } = null!;
        public string Group { get; private set; } = null!;
        public string Description { get; private set; } = null!;
        public bool IsDeprecated { get; private set; }

        public void UpdateMetadata(
            string service,
            string group,
            string description)
        {
            Service = service;
            Group = group;
            Description = description;
        }

        public void Deprecate()
        {
            IsDeprecated = true;
        }

        public void Activate()
        {
            IsDeprecated = false;
        }
    }
}
