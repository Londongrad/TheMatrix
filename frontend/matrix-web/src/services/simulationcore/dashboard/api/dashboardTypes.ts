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

export interface CityOperationsDashboardView {
    generatedAtUtc: string;
    trackedHosts: DashboardMetricView;
    readyHosts: DashboardMetricView;
    archivedRecords: DashboardMetricView;
    attentionQueue: DashboardMetricView;
    newCities: DashboardPeriodComparisonRowView;
    archivedCities: DashboardPeriodComparisonRowView;
    failedBootstraps: DashboardPeriodComparisonRowView;
    readyHandOffs: DashboardPeriodComparisonRowView;
    services: DashboardServiceHealthView[];
    events: DashboardRecentEventView[];
    attentionCities: CityListItemView[];
    readyCities: CityListItemView[];
    archivedCitiesList: CityListItemView[];
}
