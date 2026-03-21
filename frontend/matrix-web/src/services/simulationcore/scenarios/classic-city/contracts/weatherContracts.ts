export interface CityWeatherOverrideView {
    overrideId: string;
    source: string;
    reason?: string | null;
    forcedType: string;
    forcedSeverity: string;
    forcedPrecipitationKind: string;
    startsAtUtc: string;
    endsAtUtc: string;
}

export interface CityWeatherView {
    cityId: string;
    climateZone: string;
    hemisphere: string;
    utcOffsetMinutes: number;
    currentType: string;
    severity: string;
    precipitationKind: string;
    temperatureC: number;
    humidityPercent: number;
    windSpeedKph: number;
    cloudCoveragePercent: number;
    pressureHpa: number;
    startedAtUtc: string;
    expectedUntilUtc: string;
    lastEvaluatedAtUtc: string;
    lastTransitionAtUtc: string;
    activeOverride?: CityWeatherOverrideView | null;
}
