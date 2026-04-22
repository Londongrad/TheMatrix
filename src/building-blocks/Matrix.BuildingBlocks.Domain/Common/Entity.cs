namespace Matrix.BuildingBlocks.Domain.Common
{
    /// <summary>
    ///     Base type for domain entities.
    /// </summary>
    public abstract class Entity<TId>(TId id)
        where TId : notnull
    {
        public TId Id { get; } = id;

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is not Entity<TId> other || other.GetType() != GetType())
            {
                return false;
            }

            return EqualityComparer<TId>.Default.Equals(
                x: Id,
                y: other.Id);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                GetType(),
                Id);
        }
    }
}
