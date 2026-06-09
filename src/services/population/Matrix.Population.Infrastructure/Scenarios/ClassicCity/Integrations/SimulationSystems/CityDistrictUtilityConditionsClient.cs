using System.Net;
using System.Net.Http.Json;
using Matrix.Population.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Integrations.SimulationSystems
{
    internal sealed class CityDistrictUtilityConditionsClient(HttpClient client)
        : ICityDistrictUtilityConditionsClient
    {
        private readonly HttpClient _client = client;

        public async Task<IReadOnlyDictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>> GetByCityAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            Task<DistrictHeatingConditionsPayload?> heatingTask = TryGetPayloadAsync<DistrictHeatingConditionsPayload>(
                requestUri: $"/api/classic-city/cities/{cityId}/heating/districts",
                cancellationToken: cancellationToken);
            Task<DistrictWaterConditionsPayload?> waterTask = TryGetPayloadAsync<DistrictWaterConditionsPayload>(
                requestUri: $"/api/classic-city/cities/{cityId}/water-distribution/districts",
                cancellationToken: cancellationToken);
            Task<DistrictPowerConditionsPayload?> powerTask = TryGetPayloadAsync<DistrictPowerConditionsPayload>(
                requestUri: $"/api/classic-city/cities/{cityId}/power-distribution/districts",
                cancellationToken: cancellationToken);
            Task<DistrictSanitationConditionsPayload?> sanitationTask =
                TryGetPayloadAsync<DistrictSanitationConditionsPayload>(
                    requestUri: $"/api/classic-city/cities/{cityId}/sanitation/districts",
                    cancellationToken: cancellationToken);
            Task<DistrictUtilityIncidentConditionsPayload?> incidentsTask =
                TryGetPayloadAsync<DistrictUtilityIncidentConditionsPayload>(
                    requestUri: $"/api/classic-city/cities/{cityId}/utility-incidents/districts",
                    cancellationToken: cancellationToken);

            await Task.WhenAll(
                heatingTask,
                waterTask,
                powerTask,
                sanitationTask,
                incidentsTask);

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

            DistrictId[] districtIds = heatingByDistrictId.Keys
               .Concat(waterByDistrictId.Keys)
               .Concat(powerByDistrictId.Keys)
               .Concat(sanitationByDistrictId.Keys)
               .Concat(incidentsByDistrictId.Keys)
               .Distinct()
               .ToArray();

            var snapshots = new Dictionary<DistrictId, CityDistrictUtilityConditionsSnapshot>(districtIds.Length);

            foreach (DistrictId districtId in districtIds)
            {
                heatingByDistrictId.TryGetValue(
                    key: districtId,
                    value: out DistrictHeatingConditionPayload? heatingDistrict);
                waterByDistrictId.TryGetValue(
                    key: districtId,
                    value: out DistrictWaterConditionPayload? waterDistrict);
                powerByDistrictId.TryGetValue(
                    key: districtId,
                    value: out DistrictPowerConditionPayload? powerDistrict);
                sanitationByDistrictId.TryGetValue(
                    key: districtId,
                    value: out DistrictSanitationConditionPayload? sanitationDistrict);
                incidentsByDistrictId.TryGetValue(
                    key: districtId,
                    value: out DistrictUtilityIncidentConditionPayload? incidentDistrict);

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

        private async Task<TPayload?> TryGetPayloadAsync<TPayload>(
            string requestUri,
            CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await _client.GetAsync(
                requestUri: requestUri,
                cancellationToken: cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return default(TPayload?);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<TPayload>(cancellationToken: cancellationToken);
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
