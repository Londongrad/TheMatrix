namespace Matrix.BuildingBlocks.Application.Enums
{
    /// <summary>
    ///     Тип прикладочной ошибки (не привязан к HTTP напрямую).
    /// </summary>
    public enum ApplicationErrorType
    {
        Unknown = 0,

        /// <summary>
        ///     Ошибка валидации входных данных / команды.
        /// </summary>
        Validation,

        /// <summary>
        ///     Ресурс не найден.
        /// </summary>
        NotFound,

        /// <summary>
        ///     Пользователь не аутентифицирован.
        /// </summary>
        Unauthorized,

        /// <summary>
        ///     Пользователь аутентифицирован, но у него нет прав.
        /// </summary>
        Forbidden,

        /// <summary>
        ///     Конфликт состояния (например, уже существует, версия не совпадает и т.п.).
        /// </summary>
        Conflict,

        /// <summary>
        ///     Бизнес-правило на уровне Application (не Domain).
        /// </summary>
        BusinessRule
    }
}
