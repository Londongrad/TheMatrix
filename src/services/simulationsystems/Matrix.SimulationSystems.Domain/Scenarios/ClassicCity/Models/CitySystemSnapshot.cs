using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models
{
    /// <summary>
    ///     Normalized condition snapshot for a single municipal-style city system.
    /// </summary>
    public sealed class CitySystemSnapshot
    {
        public CitySystemSnapshot(
            CitySystemKind kind,
            decimal loadIndex,
            decimal serviceQualityIndex,
            decimal backlogIndex,
            decimal failureRiskIndex)
        {
            Kind = GuardHelper.AgainstInvalidEnum(
                value: kind,
                propertyName: nameof(kind));
            LoadIndex = NormalizeIndex(
                value: loadIndex,
                paramName: nameof(loadIndex));
            ServiceQualityIndex = NormalizeIndex(
                value: serviceQualityIndex,
                paramName: nameof(serviceQualityIndex));
            BacklogIndex = NormalizeIndex(
                value: backlogIndex,
                paramName: nameof(backlogIndex));
            FailureRiskIndex = NormalizeIndex(
                value: failureRiskIndex,
                paramName: nameof(failureRiskIndex));
        }

        public CitySystemKind Kind { get; }
        public decimal LoadIndex { get; }
        public decimal ServiceQualityIndex { get; }
        public decimal BacklogIndex { get; }
        public decimal FailureRiskIndex { get; }

        private static decimal NormalizeIndex(
            decimal value,
            string paramName)
        {
            if (value is < 0m or > 1m)
                throw new ArgumentOutOfRangeException(paramName);

            return decimal.Round(
                d: value,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }
    }
}
