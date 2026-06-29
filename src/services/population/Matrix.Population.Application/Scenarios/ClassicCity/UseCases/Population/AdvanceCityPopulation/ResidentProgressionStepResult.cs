namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal readonly record struct ResidentProgressionStepResult(
        bool PopulationChanged,
        int HealthcareHealthDelta)
    {
        public bool HasAnyEffect => PopulationChanged || HealthcareHealthDelta != 0;

        public static ResidentProgressionStepResult None => new(false, 0);
    }
}
