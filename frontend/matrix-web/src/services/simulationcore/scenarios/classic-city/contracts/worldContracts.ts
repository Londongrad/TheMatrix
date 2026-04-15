export interface DistrictView {
    districtId: string;
    cityId: string;
    name: string;
    anchorX: number;
    anchorY: number;
    createdAtUtc: string;
}

export interface ResidentialBuildingView {
    residentialBuildingId: string;
    cityId: string;
    districtId: string;
    accessRoadNodeId: string;
    name: string;
    type: string;
    residentCapacity: number;
    positionX: number;
    positionY: number;
    createdAtUtc: string;
}

export interface CityAnchorView {
    cityAnchorId: string;
    cityId: string;
    districtId: string;
    accessRoadNodeId: string;
    name: string;
    type: string;
    capacity: number;
    positionX: number;
    positionY: number;
    createdAtUtc: string;
}

export interface RoadNodeView {
    roadNodeId: string;
    cityId: string;
    districtId: string;
    name: string;
    type: string;
    positionX: number;
    positionY: number;
    createdAtUtc: string;
}

export interface RoadSegmentView {
    roadSegmentId: string;
    cityId: string;
    districtId: string;
    fromRoadNodeId: string;
    toRoadNodeId: string;
    name: string;
    type: string;
    lengthMeters: number;
    createdAtUtc: string;
}

export interface CityMapTopologyView {
    cityId: string;
    districts: DistrictView[];
    residentialBuildings: ResidentialBuildingView[];
    anchors: CityAnchorView[];
    roadNodes: RoadNodeView[];
    roadSegments: RoadSegmentView[];
}

export interface CityActiveTripEndpointView {
    kind: string;
    entityId: string;
    districtId: string;
    roadNodeId: string;
    name: string;
    positionX: number;
    positionY: number;
}

export interface CityActiveTripProgressView {
    districtId: string;
    roadSegmentId?: string | null;
    segmentProgressIndex: number;
    positionX: number;
    positionY: number;
}

export interface CityActiveTripView {
    tripId: string;
    cityId: string;
    travellerEntityId?: string | null;
    subject: string;
    purpose: string;
    profile: string;
    status: string;
    movementCapabilityIndex: number;
    usedDynamicRoadConditions: boolean;
    plannedAtTickId: number;
    conditionsEffectiveTickId?: number | null;
    lastAdvancedTickId: number;
    startedAtSimTimeUtc: string;
    lastAdvancedAtSimTimeUtc: string;
    expectedArrivalAtSimTimeUtc: string;
    arrivedAtSimTimeUtc?: string | null;
    currentProgressIndex: number;
    totalDistanceMeters: number;
    distanceTravelledMeters: number;
    remainingDistanceMeters: number;
    plannedTravelTimeMinutes: number;
    adjustedTravelTimeMinutes: number;
    from: CityActiveTripEndpointView;
    to: CityActiveTripEndpointView;
    current: CityActiveTripProgressView;
}
