using System.Security.Cryptography;
using System.Text;
using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Programs;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;

namespace Matrix.Education.Application.Progression
{
    public static class ProgressionEnrollmentIdFactory
    {
        public static EnrollmentId Create(
            SimulationHostId simulationHostId,
            ResidentId residentId,
            EducationStageKey stage,
            DateOnly enrolledOn,
            long tickId)
        {
            if (tickId < 0)
                throw new ArgumentOutOfRangeException(nameof(tickId));

            string identity = string.Join(
                separator: ':',
                simulationHostId.Value.ToString("N"),
                residentId.Value.ToString("N"),
                stage.Value,
                enrolledOn.DayNumber,
                tickId);
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
            Span<byte> guidBytes = stackalloc byte[16];
            hash.AsSpan(0, guidBytes.Length).CopyTo(guidBytes);

            guidBytes[7] = (byte)((guidBytes[7] & 0x0f) | 0x50);
            guidBytes[8] = (byte)((guidBytes[8] & 0x3f) | 0x80);

            return new EnrollmentId(new Guid(guidBytes));
        }
    }
}
