using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;

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
                errorFactory: ClassicCityDomainErrorsFactory.InvalidCitySystemKind);
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
            return decimal.Round(
                d: GuardHelper.AgainstOutOfRange(
                    value: value,
                    min: 0m,
                    max: 1m,
                    errorFactory: ClassicCityDomainErrorsFactory.CityNormalizedIndexOutOfRange,
                    propertyName: paramName),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }
    }
}
