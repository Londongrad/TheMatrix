using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.ValueObjects;
using PersonEntity = Matrix.Population.Domain.Entities.Person;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.AdvanceCityPopulation
{
    internal static class ResidentPlacementPoolBuilder
    {
        internal static Dictionary<EducationLevel, List<CityEducationInstitutionBinding>> BuildEducationInstitutionPools(
            IEnumerable<PersonEntity> persons)
        {
            var pools = new Dictionary<EducationLevel, List<CityEducationInstitutionBinding>>();
            foreach (PersonEntity person in persons)
            {
                if (person.Education.CurrentInstitutionId is not
                    { } institutionId)
                    continue;
                EducationLevel level = person.Education.Level;
                if (!pools.TryGetValue(
                        key: level,
                        value: out List<CityEducationInstitutionBinding>? levelPool))
                {
                    levelPool = [];
                    pools[level] = levelPool;
                }

                if (!levelPool.Any(x => x.InstitutionId == institutionId))
                    levelPool.Add(
                        new CityEducationInstitutionBinding(
                            InstitutionId: institutionId,
                            InstitutionAnchorId: person.Education.CurrentInstitutionAnchorId));
            }

            return pools;
        }

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
