using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems
{
    /// <summary>
    ///     Persisted mutable state for a single classic-city environmental system.
    /// </summary>
    public sealed class CitySystemState
    {
        private CitySystemState() { }

        private CitySystemState(
            CitySystemKind kind,
            decimal loadIndex,
            decimal serviceQualityIndex,
            decimal backlogIndex,
            decimal failureRiskIndex)
        {
            Kind = kind;
            LoadIndex = loadIndex;
            ServiceQualityIndex = serviceQualityIndex;
            BacklogIndex = backlogIndex;
            FailureRiskIndex = failureRiskIndex;
        }

        public CitySystemKind Kind { get; private set; }
        public decimal LoadIndex { get; private set; }
        public decimal ServiceQualityIndex { get; private set; }
        public decimal BacklogIndex { get; private set; }
        public decimal FailureRiskIndex { get; private set; }

        public static CitySystemState Create(CitySystemSnapshot snapshot)
        {
            GuardHelper.AgainstNull(
                value: snapshot,
                errorFactory: ClassicCityDomainErrorsFactory.CitySystemSnapshotRequired);

            return new CitySystemState(
                kind: snapshot.Kind,
                loadIndex: snapshot.LoadIndex,
                serviceQualityIndex: snapshot.ServiceQualityIndex,
                backlogIndex: snapshot.BacklogIndex,
                failureRiskIndex: snapshot.FailureRiskIndex);
        }

        public void ApplySnapshot(CitySystemSnapshot snapshot)
        {
            GuardHelper.AgainstNull(
                value: snapshot,
                errorFactory: ClassicCityDomainErrorsFactory.CitySystemSnapshotRequired);

            if (snapshot.Kind != Kind)
                throw ClassicCityDomainErrorsFactory.CitySystemSnapshotKindMismatch(
                    value: snapshot.Kind,
                    expected: Kind,
                    propertyName: nameof(snapshot));

            LoadIndex = snapshot.LoadIndex;
            ServiceQualityIndex = snapshot.ServiceQualityIndex;
            BacklogIndex = snapshot.BacklogIndex;
            FailureRiskIndex = snapshot.FailureRiskIndex;
        }

        public CitySystemSnapshot ToSnapshot()
        {
            return new CitySystemSnapshot(
                kind: Kind,
                loadIndex: LoadIndex,
                serviceQualityIndex: ServiceQualityIndex,
                backlogIndex: BacklogIndex,
                failureRiskIndex: FailureRiskIndex);
        }
    }
}
