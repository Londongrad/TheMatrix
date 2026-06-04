import type {CityListItemView} from "@services/simulationcore/scenarios/classic-city/contracts/citiesContracts";

export interface DashboardMetricView {
    label: string;
    current: number;
    description: string;
    deltaYesterday?: number | null;
    deltaMonth?: number | null;
    deltaYear?: number | null;
    deltaMode?: string | null;
}

export interface DashboardWindowComparisonView {
    current: number;
    previous: number;
    delta: number;
}

export interface DashboardPeriodComparisonRowView {
    label: string;
    description: string;
    yesterday: DashboardWindowComparisonView;
    month: DashboardWindowComparisonView;
    year: DashboardWindowComparisonView;
}

export interface DashboardServiceHealthView {
    service: string;
    status: string;
    detail: string;
    checkedAtUtc: string;
}

export interface DashboardRecentEventView {
    kind: string;
    severity: string;
    title: string;
    detail: string;
    cityId: string;
    cityName: string;
    cityStatus: string;
    occurredAtUtc: string;
}

export interface DashboardDistrictResponsePriorityView {
    cityId: string;
    cityName: string;
    cityStatus: string;
    districtId: string;
    severity: string;
    summary: string;
    recommendedFocus: string;
    priorityScore: number;
    populationPressureIndex: number;
    utilityIncidentPressureIndex: number;
    serviceDisruptionIndex: number;
    maintenancePriorityIndex: number;
    activeIllnessCount: number;
    severeIllnessCount: number;
    homelessResidentCount: number;
}

export interface DashboardEnvironmentalAlertView {
    cityId: string;
    cityName: string;
    cityStatus: string;
    severity: string;
    summary: string;
    alertScore: number;
}

export interface DashboardPopulationDistrictPressureView {
    cityId: string;
    cityName: string;
    cityStatus: string;
    districtId: string;
    severity: string;
    summary: string;
    populationPressureIndex: number;
    utilityContinuityIndex: number;
    housingFragilityIndex: number;
    residentCount: number;
    activeIllnessCount: number;
    severeIllnessCount: number;
    homelessResidentCount: number;
}

export interface DashboardMobilityTripView {
    tripId: string;
    subject: string;
    purpose: string;
    status: string;
    currentProgressIndex: number;
    usedDynamicRoadConditions: boolean;
    adjustedTravelTimeMinutes: number;
    plannedTravelTimeMinutes: number;
    from: {
        name: string;
    };
    to: {
        name: string;
    };
}

export interface DashboardMobilityView {
    cityId: string;
    cityName: string;
    cityStatus: string;
    severity: string;
    summary: string;
    mobilityPressureIndex: number;
    activeTripCount: number;
    activeCommuteCount: number;
    activeHealthcareTripCount: number;
    delayedTripCount: number;
    dynamicRoadTripCount: number;
    averageSlowdownRatio: number;
    averageRemainingTravelMinutes: number;
    trips: DashboardMobilityTripView[];
}

export interface DashboardBudgetControlCategoryView {
    category: string;
    authorizationLevel: string;
    availableAmount: number;
}

export interface DashboardBudgetControlView {
    general: DashboardBudgetControlCategoryView;
    operations: DashboardBudgetControlCategoryView;
    infrastructure: DashboardBudgetControlCategoryView;
    healthcare: DashboardBudgetControlCategoryView;
}

export interface DashboardBudgetPressureView {
    cityId: string;
    cityName: string;
    cityStatus: string;
    severity: string;
    summary: string;
    controlStatus: string;
    pressureIndex: number;
    controls: DashboardBudgetControlView;
}

export interface DashboardTickFreshnessView {
    cityId: string;
    cityName: string;
    cityStatus: string;
    severity: string;
    summary: string;
    environmentalTickId: number;
    budgetTickId: number;
    tickSkew: number;
}

export interface DashboardPhaseProgressView {
    cityId: string;
    cityName: string;
    cityStatus: string;
    severity: string;
    summary: string;
    systemsTickId: number;
    systemsPhase: string;
    resourcesTickId: number;
    resourcesPhase: string;
    budgetTickId: number;
    budgetPhase: string;
    tickSpread: number;
    laggingService: string;
    leadingService: string;
}

export interface CityOperationsDashboardView {
    generatedAtUtc: string;
    trackedHosts: DashboardMetricView;
    readyHosts: DashboardMetricView;
    archivedRecords: DashboardMetricView;
    attentionQueue: DashboardMetricView;
    environmentalAlerts: DashboardMetricView;
    populationDistrictAlerts: DashboardMetricView;
    districtResponsePriorityAlerts: DashboardMetricView;
    mobilityAlerts: DashboardMetricView;
    operationalBudgetAlerts: DashboardMetricView;
    tickFreshnessAlerts: DashboardMetricView;
    phaseProgressAlerts: DashboardMetricView;
    newCities: DashboardPeriodComparisonRowView;
    archivedCities: DashboardPeriodComparisonRowView;
    failedBootstraps: DashboardPeriodComparisonRowView;
    readyHandOffs: DashboardPeriodComparisonRowView;
    services: DashboardServiceHealthView[];
    events: DashboardRecentEventView[];
    environmentalCities: DashboardEnvironmentalAlertView[];
    populationDistrictCities: DashboardPopulationDistrictPressureView[];
    districtResponsePriorities: DashboardDistrictResponsePriorityView[];
    mobilityCities: DashboardMobilityView[];
    budgetPressureCities: DashboardBudgetPressureView[];
    tickFreshnessCities: DashboardTickFreshnessView[];
    phaseProgressCities: DashboardPhaseProgressView[];
    attentionCities: CityListItemView[];
    readyCities: CityListItemView[];
    archivedCitiesList: CityListItemView[];
}
