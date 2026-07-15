using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.ValueObjects;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class ResidentPlacementPoolBuilder
    {
        internal static Dictionary<string, List<Job>> BuildWorkplacePools(IEnumerable<PersonEntity> persons)
        {
            var pools = new Dictionary<string, List<Job>>(StringComparer.OrdinalIgnoreCase);
            foreach (PersonEntity person in persons)
            {
                if (person.Employment.Status != EmploymentStatus.Employed ||
                    person.Employment.Job is not
                    { } job)
                    continue;
                if (!pools.TryGetValue(
                        key: job.Title,
                        value: out List<Job>? titlePool))
                {
                    titlePool = [];
                    pools[job.Title] = titlePool;
                }

                if (!titlePool.Any(x => x.WorkplaceId == job.WorkplaceId))
                    titlePool.Add(job);
            }

            return pools;
        }
    }
}
