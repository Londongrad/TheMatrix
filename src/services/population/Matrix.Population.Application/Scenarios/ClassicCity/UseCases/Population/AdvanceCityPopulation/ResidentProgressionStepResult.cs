namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal readonly record struct ResidentProgressionStepResult(
        bool PopulationChanged,
        int ExternalHealthDelta)
    {
        public bool HasAnyEffect => PopulationChanged || ExternalHealthDelta != 0;

        public static ResidentProgressionStepResult None => new(false, 0);
    }
}
