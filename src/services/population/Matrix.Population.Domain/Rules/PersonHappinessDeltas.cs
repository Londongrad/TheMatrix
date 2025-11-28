namespace Matrix.Population.Domain.Rules
{
    /// <summary>
    /// Базовые изменения уровня счастья при разных жизненных событиях.
    /// Это "сырые" дельты, которые потом модифицируются Personality.
    /// </summary>
    public static class PersonHappinessDeltas
    {
        public const int OnJobAssigned = +10;
        public const int OnFired = -10;

        public const int OnRetired = +5;
        public const int OnStatusStudent = +5;
        public const int OnStatusNone = -5;

        public const int OnMarry = +15;
        public const int OnDivorce = -15;
        public const int OnWidow = -30;
    }
}
