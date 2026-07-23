using MediatR;

namespace Matrix.Population.Application.Integration.Education.ApplyEducationAttendance;

public sealed record ApplyEducationAttendanceCommand(Guid SimulationHostId, long SourceTickId,
    DateTimeOffset ObservedAtSimTimeUtc, IReadOnlyList<EducationAttendanceInput> Residents) : IRequest<int>;
