import {useMemo} from "react";
import Button from "@shared/ui/controls/Button/Button";
import Card from "@shared/ui/controls/Card/Card";
import {useCityDistrictInfrastructure} from "@services/simulationcore/scenarios/classic-city/hooks/useCityDistrictInfrastructure";
import {useCityDistrictOperatorActions} from "@services/simulationcore/scenarios/classic-city/hooks/useCityDistrictOperatorActions";
import type {
    CityDistrictHeatingConditionView,
    CityDistrictInfrastructureView,
    CityDistrictPowerDistributionConditionView,
    CityDistrictSanitationConditionView,
    CityDistrictUtilityIncidentConditionView,
    CityDistrictWaterDistributionConditionView,
} from "@services/simulationcore/scenarios/classic-city/contracts/infrastructureContracts";
import type {DistrictView} from "@services/simulationcore/scenarios/classic-city/contracts/worldContracts";
import {useCityMapTopology} from "@services/simulationcore/scenarios/classic-city/hooks/useCityMapTopology";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {usePermissions} from "@shared/permissions/usePermissions";

type Props = {
    cityId: string;
    cityName?: string;
    isArchived?: boolean;
};

type DistrictInfrastructureRow = {
    districtId: string;
    districtName: string;
    priorityIndex: number;
    heating?: CityDistrictHeatingConditionView;
    water?: CityDistrictWaterDistributionConditionView;
    power?: CityDistrictPowerDistributionConditionView;
    sanitation?: CityDistrictSanitationConditionView;
    incidents?: CityDistrictUtilityIncidentConditionView;
};

function formatDateTime(value: string | null | undefined) {
    if (!value) {
        return "--";
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return value;
    }

    return date.toLocaleString();
}

function formatIndex(value: number | null | undefined) {
    if (typeof value !== "number" || Number.isNaN(value)) {
        return "--";
    }

    return `${Math.round(value * 100)}%`;
}

function getSeverityTone(value: number) {
    if (value >= 0.7) {
        return "danger";
    }

    if (value >= 0.45) {
        return "warning";
    }

    return "success";
}

function buildDistrictLookup(districts: DistrictView[]) {
    return new Map(districts.map((district) => [district.districtId, district.name]));
}

function buildRows(
    infrastructure: CityDistrictInfrastructureView,
    districtLookup: Map<string, string>,
) {
    const rows = new Map<string, DistrictInfrastructureRow>();

    function ensure(districtId: string) {
        const existing = rows.get(districtId);
        if (existing) {
            return existing;
        }

        const created: DistrictInfrastructureRow = {
            districtId,
            districtName: districtLookup.get(districtId) ?? districtId.slice(0, 8),
            priorityIndex: 0,
        };

        rows.set(districtId, created);
        return created;
    }

    infrastructure.heating.districts.forEach((district) => {
        const row = ensure(district.districtId);
        row.heating = district;
    });

    infrastructure.waterDistribution.districts.forEach((district) => {
        const row = ensure(district.districtId);
        row.water = district;
    });

    infrastructure.powerDistribution.districts.forEach((district) => {
        const row = ensure(district.districtId);
        row.power = district;
    });

    infrastructure.sanitation.districts.forEach((district) => {
        const row = ensure(district.districtId);
        row.sanitation = district;
    });

    infrastructure.utilityIncidents.districts.forEach((district) => {
        const row = ensure(district.districtId);
        row.incidents = district;
    });

    return Array.from(rows.values())
        .map((row) => {
            const heatingRisk = row.heating
                ? (row.heating.outageRiskIndex + row.heating.comfortStressIndex + row.heating.maintenancePriorityIndex) / 3
                : 0;
            const waterRisk = row.water
                ? (row.water.disruptionRiskIndex + row.water.qualityRiskIndex + row.water.maintenancePriorityIndex) / 3
                : 0;
            const powerRisk = row.power
                ? (row.power.outageRiskIndex + row.power.restorationStrainIndex + row.power.maintenancePriorityIndex) / 3
                : 0;
            const sanitationRisk = row.sanitation
                ? (row.sanitation.overflowRiskIndex + row.sanitation.contaminationRiskIndex + row.sanitation.maintenancePriorityIndex) / 3
                : 0;
            const incidentRisk = row.incidents
                ? (row.incidents.incidentPressureIndex + row.incidents.coordinationDifficultyIndex + row.incidents.restorationPriorityIndex) / 3
                : 0;

            return {
                ...row,
                priorityIndex: (heatingRisk + waterRisk + powerRisk + sanitationRisk + incidentRisk) / 5,
            };
        })
        .sort((left, right) => right.priorityIndex - left.priorityIndex);
}

function InfrastructureRow({
    row,
    canDispatch,
    isPendingUtility,
    isPendingResupply,
    notice,
    onUtilityResponse,
    onResupply,
}: {
    row: DistrictInfrastructureRow;
    canDispatch: boolean;
    isPendingUtility: boolean;
    isPendingResupply: boolean;
    notice?: {
        tone: "success" | "warning" | "danger";
        title: string;
        detail: string;
    } | null;
    onUtilityResponse: (districtId: string) => void;
    onResupply: (districtId: string) => void;
}) {
    const tone = getSeverityTone(row.priorityIndex);

    return (
        <article className={`city-infra-row city-infra-row--${tone}`}>
            <div className="city-infra-row__topline">
                <div>
                    <h3 className="city-infra-row__title">{row.districtName}</h3>
                    <div className="city-infra-row__meta">
                        <span>District {row.districtId.slice(0, 8)}</span>
                        <span className="city-infra-row__separator">/</span>
                        <span>Priority {formatIndex(row.priorityIndex)}</span>
                    </div>
                </div>
                <div className="city-infra-row__actions">
                    <span className={`city-infra-row__priority city-infra-row__priority--${tone}`}>
                        {tone === "danger" ? "Critical" : tone === "warning" ? "Elevated" : "Stable"}
                    </span>
                    {canDispatch ? (
                        <>
                            <Button
                                size="sm"
                                variant="default"
                                onClick={() => onUtilityResponse(row.districtId)}
                                disabled={isPendingUtility || isPendingResupply}
                            >
                                {isPendingUtility ? "Dispatching..." : "Respond here"}
                            </Button>
                            <Button
                                size="sm"
                                variant="primary"
                                onClick={() => onResupply(row.districtId)}
                                disabled={isPendingUtility || isPendingResupply}
                            >
                                {isPendingResupply ? "Dispatching..." : "Resupply here"}
                            </Button>
                        </>
                    ) : null}
                </div>
            </div>

            {notice ? (
                <div className={`city-infra-row__notice city-infra-row__notice--${notice.tone}`}>
                    <strong className="city-infra-row__notice-title">{notice.title}</strong>
                    <span className="city-infra-row__notice-text">{notice.detail}</span>
                </div>
            ) : null}

            <div className="city-infra-row__grid">
                <div className="city-infra-row__metric">
                    <span className="city-infra-row__metric-label">Heating</span>
                    <strong>{formatIndex(row.heating?.heatingCoverageIndex)}</strong>
                    <span className="city-infra-row__metric-note">Stress {formatIndex(row.heating?.comfortStressIndex)}</span>
                </div>
                <div className="city-infra-row__metric">
                    <span className="city-infra-row__metric-label">Water</span>
                    <strong>{formatIndex(row.water?.waterCoverageIndex)}</strong>
                    <span className="city-infra-row__metric-note">Quality risk {formatIndex(row.water?.qualityRiskIndex)}</span>
                </div>
                <div className="city-infra-row__metric">
                    <span className="city-infra-row__metric-label">Power</span>
                    <strong>{formatIndex(row.power?.powerCoverageIndex)}</strong>
                    <span className="city-infra-row__metric-note">Outage risk {formatIndex(row.power?.outageRiskIndex)}</span>
                </div>
                <div className="city-infra-row__metric">
                    <span className="city-infra-row__metric-label">Sanitation</span>
                    <strong>{formatIndex(row.sanitation?.sanitationCoverageIndex)}</strong>
                    <span className="city-infra-row__metric-note">Overflow {formatIndex(row.sanitation?.overflowRiskIndex)}</span>
                </div>
                <div className="city-infra-row__metric">
                    <span className="city-infra-row__metric-label">Incidents</span>
                    <strong>{formatIndex(row.incidents?.utilityContinuityIndex)}</strong>
                    <span className="city-infra-row__metric-note">Pressure {formatIndex(row.incidents?.incidentPressureIndex)}</span>
                </div>
            </div>
        </article>
    );
}

export function CityInfrastructureCard({
    cityId,
    cityName,
    isArchived = false,
}: Props) {
    const infrastructureQuery = useCityDistrictInfrastructure(cityId, isArchived ? 0 : 30000);
    const topologyQuery = useCityMapTopology(cityId);
    const {can} = usePermissions();
    const canDispatch = !isArchived && can(PermissionKeys.SimulationCoreSimulationControl);
    const actions = useCityDistrictOperatorActions(cityId, async () => {
        await infrastructureQuery.refetch();
    });
    const infrastructure = infrastructureQuery.data;
    const districtLookup = useMemo(
        () => buildDistrictLookup(topologyQuery.data?.districts ?? []),
        [topologyQuery.data],
    );
    const rows = useMemo(
        () => infrastructure ? buildRows(infrastructure, districtLookup) : [],
        [districtLookup, infrastructure],
    );

    return (
        <Card
            title="District infrastructure"
            subtitle="Heating, water, power, sanitation, and incident pressure by district."
            right={(
                <Button
                    size="sm"
                    onClick={() => {
                        void Promise.all([infrastructureQuery.refetch(), topologyQuery.refetch()]);
                    }}
                    disabled={infrastructureQuery.isLoading || topologyQuery.isLoading}
                >
                    {infrastructureQuery.isRefreshing ? "Refreshing..." : infrastructureQuery.isLoading ? "Loading..." : "Refresh"}
                </Button>
            )}
        >
            {(infrastructureQuery.error || topologyQuery.error) ? (
                <div className="simulationcore-error-banner" role="alert">
                    <span>{infrastructureQuery.error ?? topologyQuery.error}</span>
                </div>
            ) : null}

            {actions.error ? (
                <div className="simulationcore-error-banner" role="alert">
                    <span>{actions.error}</span>
                    <Button size="sm" variant="default" onClick={actions.clearError}>
                        Dismiss
                    </Button>
                </div>
            ) : null}

            {infrastructureQuery.isLoading && !infrastructure ? (
                <div className="city-infra-loading" role="status" aria-live="polite">
                    <div className="city-infra-loading__title">Loading district infrastructure</div>
                    <div className="city-infra-loading__text">
                        Pulling district heating, water, power, sanitation, and utility incident slices for {cityName ?? "the selected city"}.
                    </div>
                </div>
            ) : null}

            {infrastructure ? (
                <div className="city-infra">
                    <section className="city-infra-hero">
                        <div className="city-infra-hero__content">
                            <div className="city-infra-hero__title-row">
                                <h3 className="city-infra-hero__title">{cityName ?? "Classic City"} infrastructure surface</h3>
                                <span className="city-infra-hero__badge">
                                    {isArchived ? "Archived snapshot" : "Live district view"}
                                </span>
                            </div>
                            <p className="city-infra-hero__summary">
                                Read-only district overlay built from SimulationSystems, aggregated through the gateway
                                so the operator can see where service quality is breaking down first.
                            </p>
                        </div>

                        <div className="city-infra-hero__aside">
                            <span className="city-infra-hero__aside-label">Generated</span>
                            <strong className="city-infra-hero__aside-value">{formatDateTime(infrastructure.generatedAtUtc)}</strong>
                            <span className="city-infra-hero__aside-label">Tick</span>
                            <strong className="city-infra-hero__aside-value">{infrastructure.heating.effectiveTickId}</strong>
                        </div>
                    </section>

                    <section className="city-infra-summary-grid" aria-label="District infrastructure summary">
                        <article className="city-infra-summary-card">
                            <span className="city-infra-summary-card__label">Heating support</span>
                            <strong className="city-infra-summary-card__value">{formatIndex(infrastructure.heating.heatingSupportIndex)}</strong>
                        </article>
                        <article className="city-infra-summary-card">
                            <span className="city-infra-summary-card__label">Water support</span>
                            <strong className="city-infra-summary-card__value">{formatIndex(infrastructure.waterDistribution.waterSupportIndex)}</strong>
                        </article>
                        <article className="city-infra-summary-card">
                            <span className="city-infra-summary-card__label">Power support</span>
                            <strong className="city-infra-summary-card__value">{formatIndex(infrastructure.powerDistribution.powerSupportIndex)}</strong>
                        </article>
                        <article className="city-infra-summary-card">
                            <span className="city-infra-summary-card__label">Sanitation support</span>
                            <strong className="city-infra-summary-card__value">{formatIndex(infrastructure.sanitation.sanitationSupportIndex)}</strong>
                        </article>
                        <article className="city-infra-summary-card">
                            <span className="city-infra-summary-card__label">Incident support</span>
                            <strong className="city-infra-summary-card__value">{formatIndex(infrastructure.utilityIncidents.utilityIncidentSupportIndex)}</strong>
                        </article>
                    </section>

                    <section className="city-infra-list">
                        {rows.map((row) => (
                            <InfrastructureRow
                                key={row.districtId}
                                row={row}
                                canDispatch={canDispatch}
                                isPendingUtility={
                                    actions.pendingAction?.districtId === row.districtId
                                    && actions.pendingAction.kind === "utility-response"
                                }
                                isPendingResupply={
                                    actions.pendingAction?.districtId === row.districtId
                                    && actions.pendingAction.kind === "resupply"
                                }
                                notice={actions.notice?.districtId === row.districtId ? actions.notice : null}
                                onUtilityResponse={(districtId) => {
                                    void actions.utilityResponse(districtId);
                                }}
                                onResupply={(districtId) => {
                                    void actions.resupply(districtId);
                                }}
                            />
                        ))}
                    </section>
                </div>
            ) : null}
        </Card>
    );
}
