using Matrix.Population.Domain.Entities;

namespace Matrix.Population.Application.Integration.Education
{
    public sealed class EducationParticipationProjectionIndex
    {
        private readonly IReadOnlyDictionary<Guid, EducationParticipationProjection> _projections;
        private readonly Guid _simulationHostId;

        public EducationParticipationProjectionIndex(
            Guid simulationHostId,
            IReadOnlyDictionary<Guid, EducationParticipationProjection> projections)
        {
            if (simulationHostId == Guid.Empty)
                throw new ArgumentException(
                    message: "A simulation host identifier is required.",
                    paramName: nameof(simulationHostId));

            ArgumentNullException.ThrowIfNull(projections);
            _simulationHostId = simulationHostId;
            _projections = projections;
        }

        public EducationParticipationProjection? FindCurrent(Person resident)
        {
            ArgumentNullException.ThrowIfNull(resident);

            return _projections.TryGetValue(
                       resident.Id.Value,
                       out EducationParticipationProjection? projection)
                   && projection.SimulationHostId == _simulationHostId
                   && projection.ResidentId == resident.Id.Value
                   && projection.ResidentLifecycleRevision == resident.LifecycleRevision
                ? projection
                : null;
        }
    }
}
