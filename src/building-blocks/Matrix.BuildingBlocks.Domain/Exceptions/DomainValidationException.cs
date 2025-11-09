namespace Matrix.BuildingBlocks.Domain.Exceptions
{
    /// <summary>
    /// Represents a business rule violation inside the domain model.
    /// </summary>
    public sealed class DomainValidationException(string message, string? propertyName = null) : Exception(message)
    {
        public string? PropertyName { get; } = propertyName;

        public static DomainValidationException For(string message, string propertyName)
            => new(message, propertyName);
    }
}
