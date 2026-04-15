import {useMemo} from "react";
import Card from "@shared/ui/controls/Card/Card";
import Button from "@shared/ui/controls/Button/Button";
import {useCityActiveTrips} from "@services/simulationcore/scenarios/classic-city/hooks/useCityActiveTrips";
import {useCityMapTopology} from "@services/simulationcore/scenarios/classic-city/hooks/useCityMapTopology";
import type {
    CityActiveTripView,
    CityMapTopologyView,
    RoadNodeView,
} from "@services/simulationcore/scenarios/classic-city/contracts/worldContracts";

type Props = {
    cityId: string;
    cityName?: string;
    isArchived?: boolean;
};

type Point = {
    x: number;
    y: number;
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

function formatMinutes(value: number) {
    if (value < 60) {
        return `${Math.round(value)} min`;
    }

    const hours = value / 60;
    return `${hours.toFixed(hours >= 10 ? 0 : 1)} h`;
}

function formatMeters(value: number) {
    if (value >= 1000) {
        return `${(value / 1000).toFixed(value >= 10000 ? 0 : 1)} km`;
    }

    return `${Math.round(value)} m`;
}

function humanize(value: string) {
    return value
        .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
        .replace(/[_-]+/g, " ")
        .trim()
        .replace(/\b\w/g, (match) => match.toUpperCase());
}

function getAnchorTone(type: string) {
    switch (type.trim().toLowerCase()) {
        case "hospital":
            return "hospital";
        case "school":
            return "school";
        case "workplace":
            return "workplace";
        default:
            return "default";
    }
}

function getTripTone(purpose: string) {
    switch (purpose.trim().toLowerCase()) {
        case "healthcareaccess":
        case "healthcare":
            return "healthcare";
        case "serviceresponse":
            return "response";
        case "resupply":
            return "resupply";
        default:
            return "commute";
    }
}

function buildNodeMap(topology: CityMapTopologyView) {
    return new Map<string, RoadNodeView>(topology.roadNodes.map((node) => [node.roadNodeId, node]));
}

function buildProjector(topology: CityMapTopologyView, trips: CityActiveTripView[]) {
    const allPoints: Point[] = [
        ...topology.districts.map((district) => ({x: district.anchorX, y: district.anchorY})),
        ...topology.residentialBuildings.map((building) => ({x: building.positionX, y: building.positionY})),
        ...topology.anchors.map((anchor) => ({x: anchor.positionX, y: anchor.positionY})),
        ...topology.roadNodes.map((node) => ({x: node.positionX, y: node.positionY})),
        ...trips.map((trip) => ({x: trip.current.positionX, y: trip.current.positionY})),
    ];

    const fallback = {minX: 0, minY: 0, maxX: 100, maxY: 100};
    const bounds = allPoints.reduce(
        (acc, point) => ({
            minX: Math.min(acc.minX, point.x),
            minY: Math.min(acc.minY, point.y),
            maxX: Math.max(acc.maxX, point.x),
            maxY: Math.max(acc.maxY, point.y),
        }),
        fallback,
    );

    const width = Math.max(1, bounds.maxX - bounds.minX);
    const height = Math.max(1, bounds.maxY - bounds.minY);
    const padding = 56;
    const viewWidth = 960;
    const viewHeight = 640;
    const scaleX = (viewWidth - padding * 2) / width;
    const scaleY = (viewHeight - padding * 2) / height;
    const scale = Math.min(scaleX, scaleY);

    return {
        viewBox: `0 0 ${viewWidth} ${viewHeight}`,
        project(point: Point) {
            return {
                x: padding + (point.x - bounds.minX) * scale,
                y: padding + (point.y - bounds.minY) * scale,
            };
        },
    };
}

function MapCanvas({
    topology,
    trips,
}: {
    topology: CityMapTopologyView;
    trips: CityActiveTripView[];
}) {
    const nodeMap = useMemo(() => buildNodeMap(topology), [topology]);
    const projector = useMemo(() => buildProjector(topology, trips), [topology, trips]);

    return (
        <svg
            className="city-world-map__svg"
            viewBox={projector.viewBox}
            role="img"
            aria-label="Classic City topology map"
        >
            <defs>
                <filter id="city-world-map-glow">
                    <feGaussianBlur stdDeviation="6" result="blur"/>
                    <feMerge>
                        <feMergeNode in="blur"/>
                        <feMergeNode in="SourceGraphic"/>
                    </feMerge>
                </filter>
            </defs>

            <g className="city-world-map__roads">
                {topology.roadSegments.map((segment) => {
                    const from = nodeMap.get(segment.fromRoadNodeId);
                    const to = nodeMap.get(segment.toRoadNodeId);

                    if (!from || !to) {
                        return null;
                    }

                    const fromPoint = projector.project({x: from.positionX, y: from.positionY});
                    const toPoint = projector.project({x: to.positionX, y: to.positionY});

                    return (
                        <line
                            key={segment.roadSegmentId}
                            x1={fromPoint.x}
                            y1={fromPoint.y}
                            x2={toPoint.x}
                            y2={toPoint.y}
                            className={`city-world-map__road city-world-map__road--${segment.type.toLowerCase()}`}
                        />
                    );
                })}
            </g>

            <g className="city-world-map__buildings">
                {topology.residentialBuildings.map((building) => {
                    const point = projector.project({x: building.positionX, y: building.positionY});
                    return (
                        <rect
                            key={building.residentialBuildingId}
                            x={point.x - 2}
                            y={point.y - 2}
                            width={4}
                            height={4}
                            rx={1.5}
                            className="city-world-map__building"
                        />
                    );
                })}
            </g>

            <g className="city-world-map__districts">
                {topology.districts.map((district) => {
                    const point = projector.project({x: district.anchorX, y: district.anchorY});
                    return (
                        <g key={district.districtId}>
                            <circle cx={point.x} cy={point.y} r={8} className="city-world-map__district"/>
                            <text x={point.x + 12} y={point.y - 12} className="city-world-map__district-label">
                                {district.name}
                            </text>
                        </g>
                    );
                })}
            </g>

            <g className="city-world-map__anchors">
                {topology.anchors.map((anchor) => {
                    const point = projector.project({x: anchor.positionX, y: anchor.positionY});
                    const tone = getAnchorTone(anchor.type);
                    return (
                        <circle
                            key={anchor.cityAnchorId}
                            cx={point.x}
                            cy={point.y}
                            r={5}
                            className={`city-world-map__anchor city-world-map__anchor--${tone}`}
                        />
                    );
                })}
            </g>

            <g className="city-world-map__trips" filter="url(#city-world-map-glow)">
                {trips.map((trip) => {
                    const point = projector.project({x: trip.current.positionX, y: trip.current.positionY});
                    const tone = getTripTone(trip.purpose);
                    return (
                        <circle
                            key={trip.tripId}
                            cx={point.x}
                            cy={point.y}
                            r={6}
                            className={`city-world-map__trip city-world-map__trip--${tone}`}
                        />
                    );
                })}
            </g>
        </svg>
    );
}

function TripItem({trip}: { trip: CityActiveTripView }) {
    const tone = getTripTone(trip.purpose);

    return (
        <article className={`city-world-trip city-world-trip--${tone}`}>
            <div className="city-world-trip__topline">
                <div>
                    <h3 className="city-world-trip__title">{trip.subject}</h3>
                    <div className="city-world-trip__meta">
                        <span>{trip.from.name}</span>
                        <span className="city-world-trip__separator">→</span>
                        <span>{trip.to.name}</span>
                    </div>
                </div>
                <span className={`city-world-trip__purpose city-world-trip__purpose--${tone}`}>
                    {humanize(trip.purpose)}
                </span>
            </div>

            <div className="city-world-trip__stats">
                <div>
                    <span className="city-world-trip__stat-label">Progress</span>
                    <strong>{Math.round(trip.currentProgressIndex * 100)}%</strong>
                </div>
                <div>
                    <span className="city-world-trip__stat-label">Remaining</span>
                    <strong>{formatMeters(trip.remainingDistanceMeters)}</strong>
                </div>
                <div>
                    <span className="city-world-trip__stat-label">ETA</span>
                    <strong>{formatDateTime(trip.expectedArrivalAtSimTimeUtc)}</strong>
                </div>
                <div>
                    <span className="city-world-trip__stat-label">Travel</span>
                    <strong>{formatMinutes(trip.adjustedTravelTimeMinutes)}</strong>
                </div>
            </div>

            <div className="city-world-trip__footer">
                <span>Status {humanize(trip.status)}</span>
                <span className="city-world-trip__separator">/</span>
                <span>Profile {humanize(trip.profile)}</span>
                <span className="city-world-trip__separator">/</span>
                <span>Capability {Math.round(trip.movementCapabilityIndex * 100)}%</span>
                {trip.usedDynamicRoadConditions ? (
                    <>
                        <span className="city-world-trip__separator">/</span>
                        <span>Dynamic roads</span>
                    </>
                ) : null}
            </div>
        </article>
    );
}

export function CityWorldMapCard({
    cityId,
    cityName,
    isArchived = false,
}: Props) {
    const topologyQuery = useCityMapTopology(cityId);
    const tripsQuery = useCityActiveTrips(cityId, isArchived ? 0 : 15000);
    const topology = topologyQuery.data;
    const trips = tripsQuery.data;
    const displayedTrips = trips.slice(0, 8);

    return (
        <Card
            title="Map & mobility"
            subtitle="Live topology, travel surface, and active world trips for the current city."
            right={(
                <Button
                    size="sm"
                    onClick={() => {
                        void Promise.all([topologyQuery.refetch(), tripsQuery.refetch()]);
                    }}
                    disabled={topologyQuery.isLoading || tripsQuery.isLoading}
                >
                    {topologyQuery.isLoading || tripsQuery.isLoading ? "Refreshing..." : "Refresh"}
                </Button>
            )}
        >
            {(topologyQuery.error || tripsQuery.error) ? (
                <div className="simulationcore-error-banner" role="alert">
                    <span>{topologyQuery.error ?? tripsQuery.error}</span>
                </div>
            ) : null}

            {topologyQuery.isLoading && !topology ? (
                <div className="city-world-loading" role="status" aria-live="polite">
                    <div className="city-world-loading__title">Loading city map</div>
                    <div className="city-world-loading__text">
                        Building the topology and active travel layer for {cityName ?? "the selected city"}.
                    </div>
                </div>
            ) : null}

            {topology ? (
                <div className="city-world">
                    <section className="city-world-hero">
                        <div className="city-world-hero__content">
                            <div className="city-world-hero__title-row">
                                <h3 className="city-world-hero__title">{cityName ?? "Classic City"} world surface</h3>
                                <span className="city-world-hero__badge">
                                    {isArchived ? "Archived topology" : "Live mobility"}
                                </span>
                            </div>
                            <p className="city-world-hero__summary">
                                Canonical city topology from SimulationCore, with real road graph nodes, anchors, and
                                current travel state overlaid on top.
                            </p>
                        </div>

                        <div className="city-world-hero__stats">
                            <div className="city-world-hero__stat">
                                <span className="city-world-hero__stat-label">Districts</span>
                                <strong>{topology.districts.length}</strong>
                            </div>
                            <div className="city-world-hero__stat">
                                <span className="city-world-hero__stat-label">Road segments</span>
                                <strong>{topology.roadSegments.length}</strong>
                            </div>
                            <div className="city-world-hero__stat">
                                <span className="city-world-hero__stat-label">Anchors</span>
                                <strong>{topology.anchors.length}</strong>
                            </div>
                            <div className="city-world-hero__stat">
                                <span className="city-world-hero__stat-label">Active trips</span>
                                <strong>{trips.length}</strong>
                            </div>
                        </div>
                    </section>

                    <div className="city-world-grid">
                        <section className="city-world-map">
                            <div className="city-world-map__header">
                                <h3 className="city-world-map__title">Topology canvas</h3>
                                <div className="city-world-map__legend">
                                    <span className="city-world-map__legend-item city-world-map__legend-item--district">District</span>
                                    <span className="city-world-map__legend-item city-world-map__legend-item--anchor">Anchor</span>
                                    <span className="city-world-map__legend-item city-world-map__legend-item--trip">Trip</span>
                                </div>
                            </div>

                            <div className="city-world-map__frame">
                                <MapCanvas topology={topology} trips={trips}/>
                            </div>
                        </section>

                        <section className="city-world-side">
                            <div className="city-world-side__panel">
                                <div className="city-world-side__header">
                                    <h3 className="city-world-side__title">Active travel feed</h3>
                                    <span className="city-world-side__count">{trips.length}</span>
                                </div>

                                {displayedTrips.length === 0 ? (
                                    <div className="city-world-empty" role="status">
                                        <div className="city-world-empty__text">
                                            No active world trips are being tracked right now.
                                        </div>
                                    </div>
                                ) : (
                                    <div className="city-world-trip-list">
                                        {displayedTrips.map((trip) => (
                                            <TripItem key={trip.tripId} trip={trip}/>
                                        ))}
                                    </div>
                                )}
                            </div>

                            <div className="city-world-side__panel">
                                <div className="city-world-side__header">
                                    <h3 className="city-world-side__title">Static surface</h3>
                                </div>
                                <div className="city-world-surface-list">
                                    <div className="city-world-surface-item">
                                        <span className="city-world-surface-item__label">Residential buildings</span>
                                        <strong>{topology.residentialBuildings.length}</strong>
                                    </div>
                                    <div className="city-world-surface-item">
                                        <span className="city-world-surface-item__label">Road nodes</span>
                                        <strong>{topology.roadNodes.length}</strong>
                                    </div>
                                    <div className="city-world-surface-item">
                                        <span className="city-world-surface-item__label">Hospitals / schools / workplaces</span>
                                        <strong>{topology.anchors.length}</strong>
                                    </div>
                                    <div className="city-world-surface-item">
                                        <span className="city-world-surface-item__label">Topology city id</span>
                                        <strong className="city-world-surface-item__mono">{topology.cityId.slice(0, 8)}</strong>
                                    </div>
                                </div>
                            </div>
                        </section>
                    </div>
                </div>
            ) : null}
        </Card>
    );
}
