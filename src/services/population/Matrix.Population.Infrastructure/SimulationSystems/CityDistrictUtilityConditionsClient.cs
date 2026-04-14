using System.Net.Http.Json;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Infrastructure.SimulationSystems
{
    internal sealed class CityDistrictUtilityConditionsClient(HttpClient client)
        : ICityDistrictUtilityConditionsClient
    {
        private readonly HttpClient _client = client;

        public async Task<IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>> GetByCityAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            Task<DistrictHeatingConditionsPayload?> heatingTask = _client.GetFromJsonAsync<DistrictHeatingConditionsPayload>(
                requestUri: $"/api/classic-city/cities/{cityId}/heating/districts",
                cancellationToken: cancellationToken);
            Task<DistrictWaterConditionsPayload?> waterTask = _client.GetFromJsonAsync<DistrictWaterConditionsPayload>(
                requestUri: $"/api/classic-city/cities/{cityId}/water-distribution/districts",
                cancellationToken: cancellationToken);
            Task<DistrictPowerConditionsPayload?> powerTask = _client.GetFromJsonAsync<DistrictPowerConditionsPayload>(
                requestUri: $"/api/classic-city/cities/{cityId}/power-distribution/districts",
                cancellationToken: cancellationToken);
            Task<DistrictSanitationConditionsPayload?> sanitationTask = _client.GetFromJsonAsync<DistrictSanitationConditionsPayload>(
                requestUri: $"/api/classic-city/cities/{cityId}/sanitation/districts",
                cancellationToken: cancellationToken);
            Task<DistrictUtilityIncidentConditionsPayload?> incidentsTask = _client.GetFromJsonAsync<DistrictUtilityIncidentConditionsPayload>(
                requestUri: $"/api/classic-city/cities/{cityId}/utility-incidents/districts",
                cancellationToken: cancellationToken);

            await Task.WhenAll(heatingTask, waterTask, powerTask, sanitationTask, incidentsTask);

            DistrictHeatingConditionsPayload? heating = await heatingTask;
            DistrictWaterConditionsPayload? water = await waterTask;
            DistrictPowerConditionsPayload? power = await powerTask;
            DistrictSanitationConditionsPayload? sanitation = await sanitationTask;
            DistrictUtilityIncidentConditionsPayload? incidents = await incidentsTask;

            if (heating is null || water is null || power is null || sanitation is null || incidents is null)
                return new Dictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>();

            var heatingByDistrictId = heating.Districts.ToDictionary(x => DistrictId.From(x.DistrictId));
            var waterByDistrictId = water.Districts.ToDictionary(x => DistrictId.From(x.DistrictId));
            var powerByDistrictId = power.Districts.ToDictionary(x => DistrictId.From(x.DistrictId));
            var sanitationByDistrictId = sanitation.Districts.ToDictionary(x => DistrictId.From(x.DistrictId));
            var incidentsByDistrictId = incidents.Districts.ToDictionary(x => DistrictId.From(x.DistrictId));

            var districtIds = heatingByDistrictId.Keys
                .Concat(waterByDistrictId.Keys)
                .Concat(powerByDistrictId.Keys)
                .Concat(sanitationByDistrictId.Keys)
                .Concat(incidentsByDistrictId.Keys)
                .Distinct()
                .ToArray();

            var snapshots = new Dictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>(districtIds.Length);

            foreach (DistrictId districtId in districtIds)
            {
                heatingByDistrictId.TryGetValue(districtId, out DistrictHeatingConditionPayload? heatingDistrict);
                waterByDistrictId.TryGetValue(districtId, out DistrictWaterConditionPayload? waterDistrict);
                powerByDistrictId.TryGetValue(districtId, out DistrictPowerConditionPayload? powerDistrict);
                sanitationByDistrictId.TryGetValue(districtId, out DistrictSanitationConditionPayload? sanitationDistrict);
                incidentsByDistrictId.TryGetValue(districtId, out DistrictUtilityIncidentConditionPayload? incidentDistrict);

                snapshots[districtId] = new CityDistrictUtilityConditionsSnapshot(
                    DistrictId: districtId,
                    HeatingCoverageIndex: heatingDistrict?.HeatingCoverageIndex ?? 1m,
                    HeatingComfortStressIndex: heatingDistrict?.ComfortStressIndex ?? 0m,
                    WaterCoverageIndex: waterDistrict?.WaterCoverageIndex ?? 1m,
                    WaterDisruptionRiskIndex: waterDistrict?.DisruptionRiskIndex ?? 0m,
                    PowerCoverageIndex: powerDistrict?.PowerCoverageIndex ?? 1m,
                    PowerOutageRiskIndex: powerDistrict?.OutageRiskIndex ?? 0m,
                    SanitationCoverageIndex: sanitationDistrict?.SanitationCoverageIndex ?? 1m,
                    SanitationContaminationRiskIndex: sanitationDistrict?.ContaminationRiskIndex ?? 0m,
                    UtilityIncidentDispatchReadinessIndex: incidentDistrict?.DispatchReadinessIndex ?? 1m,
                    UtilityIncidentPressureIndex: incidentDistrict?.IncidentPressureIndex ?? 0m,
                    UtilityIncidentCoordinationDifficultyIndex: incidentDistrict?.CoordinationDifficultyIndex ?? 0m,
                    UtilityIncidentRestorationPriorityIndex: incidentDistrict?.RestorationPriorityIndex ?? 0m);
            }

            return snapshots;
        }

        private sealed record DistrictHeatingConditionsPayload(
            Guid CityId,
            long EffectiveTickId,
            DateTimeOffset LastEvaluatedAtUtc,
            decimal HeatingSupportIndex,
            IReadOnlyList<DistrictHeatingConditionPayload> Districts);

        private sealed record DistrictHeatingConditionPayload(
            Guid DistrictId,
            decimal HeatingCoverageIndex,
            decimal HeatingSupportIndex,
            decimal OutageRiskIndex,
            decimal ComfortStressIndex,
            decimal MaintenancePriorityIndex);

        private sealed record DistrictWaterConditionsPayload(
            Guid CityId,
            long EffectiveTickId,
            DateTimeOffset LastEvaluatedAtUtc,
            decimal WaterSupportIndex,
            IReadOnlyList<DistrictWaterConditionPayload> Districts);

        private sealed record DistrictWaterConditionPayload(
            Guid DistrictId,
            decimal WaterCoverageIndex,
            decimal WaterSupportIndex,
            decimal DisruptionRiskIndex,
            decimal QualityRiskIndex,
            decimal MaintenancePriorityIndex);

        private sealed record DistrictPowerConditionsPayload(
            Guid CityId,
            long EffectiveTickId,
            DateTimeOffset LastEvaluatedAtUtc,
            decimal PowerSupportIndex,
            IReadOnlyList<DistrictPowerConditionPayload> Districts);

        private sealed record DistrictPowerConditionPayload(
            Guid DistrictId,
            decimal PowerCoverageIndex,
            decimal PowerSupportIndex,
            decimal OutageRiskIndex,
            decimal RestorationStrainIndex,
            decimal MaintenancePriorityIndex);

        private sealed record DistrictSanitationConditionsPayload(
            Guid CityId,
            long EffectiveTickId,
            DateTimeOffset LastEvaluatedAtUtc,
            decimal SanitationSupportIndex,
            IReadOnlyList<DistrictSanitationConditionPayload> Districts);

        private sealed record DistrictSanitationConditionPayload(
            Guid DistrictId,
            decimal SanitationCoverageIndex,
            decimal SanitationSupportIndex,
            decimal OverflowRiskIndex,
            decimal ContaminationRiskIndex,
            decimal MaintenancePriorityIndex);

        private sealed record DistrictUtilityIncidentConditionsPayload(
            Guid CityId,
            long EffectiveTickId,
            DateTimeOffset LastEvaluatedAtUtc,
            decimal UtilityIncidentSupportIndex,
            IReadOnlyList<DistrictUtilityIncidentConditionPayload> Districts);

        private sealed record DistrictUtilityIncidentConditionPayload(
            Guid DistrictId,
            decimal UtilityContinuityIndex,
            decimal DispatchReadinessIndex,
            decimal IncidentPressureIndex,
            decimal CoordinationDifficultyIndex,
            decimal RestorationPriorityIndex);
    }
}
