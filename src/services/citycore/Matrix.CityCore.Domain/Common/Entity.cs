namespace Matrix.CityCore.Domain.Common
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
            return obj is Entity<TId> other &&
                   EqualityComparer<TId>.Default.Equals(
                       x: Id,
                       y: other.Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
