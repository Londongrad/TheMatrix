namespace Matrix.BuildingBlocks.Domain.Exceptions
{
    /// <summary>
    /// Represents a business rule violation inside the domain model.
    /// </summary>
    public sealed class DomainException(
        string code,
        string message,
        string? propertyName = null,
        Exception? innerException = null) : Exception(message, innerException)
    {
        /// <summary>
        /// Machine-readable error code. Например: "Population.Person.ChildCannotBeEmployed".
        /// </summary>
        public string Code { get; } = string.IsNullOrWhiteSpace(code)
                ? "Domain.ValidationError"
                : code;

        /// <summary>
        /// Опциональное имя свойства/поля, которого касается ошибка.
        /// Например: "EmploymentStatus", "BirthDate" и т.п.
        /// </summary>
        public string? PropertyName { get; } = string.IsNullOrWhiteSpace(propertyName)
                ? null
                : propertyName;

        /// <summary>
        /// Упрощённый конструктор — если нужно выбросить исключение напрямую,
        /// без явного кода. Код по умолчанию будет "Domain.ValidationError".
        /// </summary>
        public DomainException(string message, string? propertyName = null)
            : this("Domain.ValidationError", message, propertyName) { }

        /// <summary>
        /// Удобное представление в логах: с кодом и именем свойства.
        /// </summary>
        public override string ToString()
        {
            var propertyPart = PropertyName is null
                ? string.Empty
                : $" (Property: {PropertyName})";

            return $"[{GetType().Name}]: [{Code}][{propertyPart}] [{Message}]";
        }
    }
}
