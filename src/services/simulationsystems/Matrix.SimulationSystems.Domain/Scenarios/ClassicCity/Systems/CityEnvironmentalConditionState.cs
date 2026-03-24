using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Common;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.SimulationSystems.Domain.Simulation;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems
{
    /// <summary>
    ///     Aggregate root for physical city conditions driven by weather pressure and system response.
    /// </summary>
    public sealed class CityEnvironmentalConditionState : AggregateRoot<SimulationHostId>
    {
        private CityEnvironmentalConditionState(
            SimulationHostId simulationHostId,
            CitySystemState drainage,
            CityDrainageInfrastructureState drainageInfrastructure,
            CitySystemState snowRemoval,
            CitySnowRemovalInfrastructureState snowRemovalInfrastructure,
            CitySystemState roadAccess,
            CityRoadAccessInfrastructureState roadAccessInfrastructure,
            CityWeatherPressureProfile weatherPressure,
            FloodingIndex floodingIndex,
            SnowAccumulationIndex snowAccumulationIndex,
            RoadAccessibilityIndex roadAccessibilityIndex,
            DateTimeOffset lastEvaluatedAtUtc)
            : base(simulationHostId)
        {
            Drainage = drainage;
            DrainageInfrastructure = drainageInfrastructure;
            SnowRemoval = snowRemoval;
            SnowRemovalInfrastructure = snowRemovalInfrastructure;
            RoadAccess = roadAccess;
            RoadAccessInfrastructure = roadAccessInfrastructure;
            WeatherPressure = weatherPressure;
            FloodingIndex = floodingIndex;
            SnowAccumulationIndex = snowAccumulationIndex;
            RoadAccessibilityIndex = roadAccessibilityIndex;
            LastEvaluatedAtUtc = EnsureUtc(
                value: lastEvaluatedAtUtc,
                paramName: nameof(lastEvaluatedAtUtc));
        }

        private CityEnvironmentalConditionState()
            : base(default(SimulationHostId))
        {
            Drainage = null!;
            DrainageInfrastructure = null!;
            SnowRemoval = null!;
            SnowRemovalInfrastructure = null!;
            RoadAccess = null!;
            RoadAccessInfrastructure = null!;
            WeatherPressure = null!;
        }

        public SimulationHostId SimulationHostId => Id;
        public CitySystemState Drainage { get; private set; }
        public CityDrainageInfrastructureState DrainageInfrastructure { get; private set; }
        public CitySystemState SnowRemoval { get; private set; }
        public CitySnowRemovalInfrastructureState SnowRemovalInfrastructure { get; private set; }
        public CitySystemState RoadAccess { get; private set; }
        public CityRoadAccessInfrastructureState RoadAccessInfrastructure { get; private set; }
        public CityWeatherPressureProfile WeatherPressure { get; private set; }
        public FloodingIndex FloodingIndex { get; private set; }
        public SnowAccumulationIndex SnowAccumulationIndex { get; private set; }
        public RoadAccessibilityIndex RoadAccessibilityIndex { get; private set; }
        public DateTimeOffset LastEvaluatedAtUtc { get; private set; }

        public static CityEnvironmentalConditionState Create(
            SimulationHostId simulationHostId,
            CityEnvironmentalConditionSnapshot seed)
        {
            GuardHelper.AgainstNull(
                value: seed,
                errorFactory: ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSnapshotRequired);

            return new CityEnvironmentalConditionState(
                simulationHostId: simulationHostId,
                drainage: CitySystemState.Create(seed.Drainage),
                drainageInfrastructure: CityDrainageInfrastructureState.Create(seed.DrainageInfrastructure),
                snowRemoval: CitySystemState.Create(seed.SnowRemoval),
                snowRemovalInfrastructure: CitySnowRemovalInfrastructureState.Create(seed.SnowRemovalInfrastructure),
                roadAccess: CitySystemState.Create(seed.RoadAccess),
                roadAccessInfrastructure: CityRoadAccessInfrastructureState.Create(seed.RoadAccessInfrastructure),
                weatherPressure: CityWeatherPressureProfile.Neutral(),
                floodingIndex: seed.FloodingIndex,
                snowAccumulationIndex: seed.SnowAccumulationIndex,
                roadAccessibilityIndex: seed.RoadAccessibilityIndex,
                lastEvaluatedAtUtc: seed.EvaluatedAtUtc);
        }

        public void ApplyWeatherPressure(CityWeatherPressureProfile weatherPressure)
        {
            GuardHelper.AgainstNull(
                value: weatherPressure,
                errorFactory: ClassicCityDomainErrorsFactory.CityWeatherPressureProfileRequired);

            WeatherPressure = weatherPressure;
        }

        public void ApplySnapshot(CityEnvironmentalConditionSnapshot snapshot)
        {
            GuardHelper.AgainstNull(
                value: snapshot,
                errorFactory: ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSnapshotRequired);

            if (snapshot.EvaluatedAtUtc < LastEvaluatedAtUtc)
                throw ClassicCityDomainErrorsFactory.CityEnvironmentalConditionSnapshotCannotMoveBackwards(
                    value: snapshot.EvaluatedAtUtc,
                    previous: LastEvaluatedAtUtc,
                    propertyName: nameof(snapshot));

            Drainage.ApplySnapshot(snapshot.Drainage);
            DrainageInfrastructure.ApplySnapshot(snapshot.DrainageInfrastructure);
            SnowRemoval.ApplySnapshot(snapshot.SnowRemoval);
            SnowRemovalInfrastructure.ApplySnapshot(snapshot.SnowRemovalInfrastructure);
            RoadAccess.ApplySnapshot(snapshot.RoadAccess);
            RoadAccessInfrastructure.ApplySnapshot(snapshot.RoadAccessInfrastructure);
            FloodingIndex = snapshot.FloodingIndex;
            SnowAccumulationIndex = snapshot.SnowAccumulationIndex;
            RoadAccessibilityIndex = snapshot.RoadAccessibilityIndex;
            LastEvaluatedAtUtc = snapshot.EvaluatedAtUtc;
        }

        public CityEnvironmentalConditionSnapshot ToSnapshot()
        {
            return new CityEnvironmentalConditionSnapshot(
                drainage: Drainage.ToSnapshot(),
                drainageInfrastructure: DrainageInfrastructure.ToSnapshot(),
                snowRemoval: SnowRemoval.ToSnapshot(),
                snowRemovalInfrastructure: SnowRemovalInfrastructure.ToSnapshot(),
                roadAccess: RoadAccess.ToSnapshot(),
                roadAccessInfrastructure: RoadAccessInfrastructure.ToSnapshot(),
                floodingIndex: FloodingIndex,
                snowAccumulationIndex: SnowAccumulationIndex,
                roadAccessibilityIndex: RoadAccessibilityIndex,
                evaluatedAtUtc: LastEvaluatedAtUtc);
        }

        public void SetDrainageEmergencyMode(bool enabled)
        {
            DrainageInfrastructure.SetEmergencyMode(enabled);
        }

        public void DispatchDrainageMaintenance(
            DrainageMaintenanceFocus focus,
            DrainageMaintenanceIntensity intensity)
        {
            DrainageInfrastructure.DispatchMaintenance(
                focus: focus,
                intensity: intensity);
        }

        public void SetSnowRemovalEmergencyMode(bool enabled)
        {
            SnowRemovalInfrastructure.SetEmergencyMode(enabled);
        }

        public void DispatchSnowRemovalMaintenance(
            SnowRemovalMaintenanceFocus focus,
            SnowRemovalMaintenanceIntensity intensity)
        {
            SnowRemovalInfrastructure.DispatchMaintenance(
                focus: focus,
                intensity: intensity);
        }

        public void SetRoadAccessEmergencyMode(bool enabled)
        {
            RoadAccessInfrastructure.SetEmergencyMode(enabled);
        }

        public void DispatchRoadAccessMaintenance(
            RoadAccessMaintenanceFocus focus,
            RoadAccessMaintenanceIntensity intensity)
        {
            RoadAccessInfrastructure.DispatchMaintenance(
                focus: focus,
                intensity: intensity);
        }

        private static DateTimeOffset EnsureUtc(
            DateTimeOffset value,
            string paramName)
        {
            return value.Offset == TimeSpan.Zero
                ? value
                : throw ClassicCityDomainErrorsFactory.CityEnvironmentalTimestampMustBeUtc(
                    value: value,
                    propertyName: paramName);
        }
    }
}
