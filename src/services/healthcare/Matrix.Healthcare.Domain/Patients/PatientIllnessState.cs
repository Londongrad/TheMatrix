namespace Matrix.Healthcare.Domain.Patients
{
    public sealed class PatientIllnessState
    {
        private PatientIllnessState()
        {
        }

        private PatientIllnessState(
            IllnessKind? currentKind,
            IllnessSeverity? currentSeverity,
            DateOnly? diagnosedOn,
            DateOnly? lastRecoveredOn)
        {
            EnsureConsistentActiveState(currentKind, currentSeverity, diagnosedOn);

            if (diagnosedOn.HasValue && lastRecoveredOn.HasValue && diagnosedOn.Value < lastRecoveredOn.Value)
                throw new ArgumentException("An active illness cannot predate the last recovery.");

            CurrentKind = currentKind;
            CurrentSeverity = currentSeverity;
            DiagnosedOn = diagnosedOn;
            LastRecoveredOn = lastRecoveredOn;
        }

        public IllnessKind? CurrentKind { get; private set; }
        public IllnessSeverity? CurrentSeverity { get; private set; }
        public DateOnly? DiagnosedOn { get; private set; }
        public DateOnly? LastRecoveredOn { get; private set; }

        public bool HasActiveIllness => CurrentKind.HasValue;

        public static PatientIllnessState Healthy(DateOnly? lastRecoveredOn = null)
        {
            return new PatientIllnessState(
                currentKind: null,
                currentSeverity: null,
                diagnosedOn: null,
                lastRecoveredOn: lastRecoveredOn);
        }

        public static PatientIllnessState Active(
            IllnessKind kind,
            IllnessSeverity severity,
            DateOnly diagnosedOn,
            DateOnly? lastRecoveredOn = null)
        {
            EnsureDefined(kind, nameof(kind));
            EnsureDefined(severity, nameof(severity));

            return new PatientIllnessState(
                currentKind: kind,
                currentSeverity: severity,
                diagnosedOn: diagnosedOn,
                lastRecoveredOn: lastRecoveredOn);
        }

        public PatientIllnessState Diagnose(
            IllnessKind kind,
            IllnessSeverity severity,
            DateOnly currentDate)
        {
            EnsureDefined(kind, nameof(kind));
            EnsureDefined(severity, nameof(severity));

            DateOnly diagnosedOn = HasActiveIllness && CurrentKind == kind
                ? DiagnosedOn!.Value
                : currentDate;

            return Active(
                kind: kind,
                severity: severity,
                diagnosedOn: diagnosedOn,
                lastRecoveredOn: LastRecoveredOn);
        }

        public PatientIllnessState ProgressTo(IllnessSeverity severity)
        {
            EnsureDefined(severity, nameof(severity));

            if (!HasActiveIllness)
                return this;

            IllnessSeverity resolvedSeverity = CurrentSeverity!.Value > severity
                ? CurrentSeverity.Value
                : severity;

            return Active(
                kind: CurrentKind!.Value,
                severity: resolvedSeverity,
                diagnosedOn: DiagnosedOn!.Value,
                lastRecoveredOn: LastRecoveredOn);
        }

        public PatientIllnessState Recover(DateOnly currentDate)
        {
            if (DiagnosedOn.HasValue && currentDate < DiagnosedOn.Value)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(currentDate),
                    message: "A patient cannot recover before the active illness was diagnosed.");

            return Healthy(lastRecoveredOn: currentDate);
        }

        private static void EnsureConsistentActiveState(
            IllnessKind? currentKind,
            IllnessSeverity? currentSeverity,
            DateOnly? diagnosedOn)
        {
            bool allActiveValuesPresent = currentKind.HasValue
                && currentSeverity.HasValue
                && diagnosedOn.HasValue;
            bool allActiveValuesMissing = !currentKind.HasValue
                && !currentSeverity.HasValue
                && !diagnosedOn.HasValue;

            if (!allActiveValuesPresent && !allActiveValuesMissing)
                throw new InvalidOperationException(
                    "Illness kind, severity, and diagnosis date must be present together.");
        }

        private static void EnsureDefined<TEnum>(TEnum value, string parameterName)
            where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
