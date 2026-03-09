using Matrix.Population.Domain.Enums;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed class IllnessInfo
    {
        private IllnessInfo() { }

        private IllnessInfo(
            IllnessKind? currentKind,
            IllnessSeverity? currentSeverity,
            DateOnly? diagnosedOn,
            DateOnly? lastRecoveredOn)
        {
            if (currentKind.HasValue != currentSeverity.HasValue)
                throw new InvalidOperationException("Illness severity must match illness kind presence.");

            if (currentKind is null && diagnosedOn.HasValue)
                throw new InvalidOperationException("Healthy illness state cannot keep diagnosed date.");

            CurrentKind = currentKind;
            CurrentSeverity = currentSeverity;
            DiagnosedOn = diagnosedOn;
            LastRecoveredOn = lastRecoveredOn;
        }

        public IllnessKind? CurrentKind { get; private set; }
        public IllnessSeverity? CurrentSeverity { get; private set; }
        public DateOnly? DiagnosedOn { get; private set; }
        public DateOnly? LastRecoveredOn { get; private set; }

        public bool HasActiveIllness => CurrentKind.HasValue && CurrentSeverity.HasValue;

        public static IllnessInfo Healthy(DateOnly? lastRecoveredOn = null)
        {
            return new IllnessInfo(
                currentKind: null,
                currentSeverity: null,
                diagnosedOn: null,
                lastRecoveredOn: lastRecoveredOn);
        }

        public IllnessInfo Diagnose(
            IllnessKind kind,
            IllnessSeverity severity,
            DateOnly currentDate)
        {
            return new IllnessInfo(
                currentKind: kind,
                currentSeverity: severity,
                diagnosedOn: HasActiveIllness && CurrentKind == kind
                    ? DiagnosedOn ?? currentDate
                    : currentDate,
                lastRecoveredOn: LastRecoveredOn);
        }

        public IllnessInfo ProgressTo(
            IllnessSeverity severity)
        {
            if (!HasActiveIllness || CurrentKind is null)
                return this;

            IllnessSeverity resolvedSeverity = CurrentSeverity.HasValue && CurrentSeverity.Value > severity
                ? CurrentSeverity.Value
                : severity;

            return new IllnessInfo(
                currentKind: CurrentKind,
                currentSeverity: resolvedSeverity,
                diagnosedOn: DiagnosedOn,
                lastRecoveredOn: LastRecoveredOn);
        }

        public IllnessInfo Recover(DateOnly currentDate)
        {
            return Healthy(lastRecoveredOn: currentDate);
        }

        public IllnessInfo ClearActive()
        {
            return Healthy(lastRecoveredOn: LastRecoveredOn);
        }
    }
}
