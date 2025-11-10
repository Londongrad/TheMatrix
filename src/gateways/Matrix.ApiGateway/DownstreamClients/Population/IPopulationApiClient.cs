namespace Matrix.ApiGateway.DownstreamClients.Population
{
    public interface IPopulationApiClient
    {
        /// <summary>«апускает расчЄт мес€чного дохода.</summary>
        Task TriggerMonthlyIncomeAsync(int month, CancellationToken cancellationToken = default);

        /// <summary>»нициализирует/пересоздаЄт попул€цию.</summary>
        Task InitializePopulationAsync(
            int peopleCount,
            int? randomSeed = null,
            CancellationToken cancellationToken = default);

        /// <summary>Health-check сервиса Population.</summary>
        Task<bool> HealthAsync(CancellationToken cancellationToken = default);
    }
}