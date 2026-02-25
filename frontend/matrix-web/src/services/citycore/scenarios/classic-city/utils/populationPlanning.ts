import type {
    ClassicCityPopulationOccupancyProfile,
    ClassicCitySetupDraftView,
} from "@services/citycore/scenarios/classic-city/contracts/setupSessionContracts";

type Range = {
    min: number;
    max: number;
};

type BuildingType = "house" | "apartment" | "tower" | "dormitory";

type BuildingWeights = Record<BuildingType, number>;

export type PopulationPlanningEstimate = {
    districtRange: Range;
    residentialBuildingRange: Range;
    capacityRange: Range;
    openingPopulationRange: Range;
    occupancyRateRange: Range;
    usesExactOverride: boolean;
};

type PopulationPlanningInput = Pick<
    ClassicCitySetupDraftView,
    "sizeTier" | "urbanDensity" | "developmentLevel" | "populationOccupancyProfile" | "usePopulationOverride" | "plannedPeopleCount"
>;

const BUILDING_CAPACITY_RANGES: Record<BuildingType, Range> = {
    house: {min: 4, max: 10},
    apartment: {min: 70, max: 180},
    tower: {min: 180, max: 360},
    dormitory: {min: 120, max: 260},
};

export function buildPopulationPlanningEstimate(input: PopulationPlanningInput): PopulationPlanningEstimate {
    const districtRange = getDistrictCountRange(input);
    const buildingRange = getResidentialBuildingRange(input, districtRange);
    const capacityRange = getCapacityRange(input, districtRange);

    if (input.usePopulationOverride) {
        const requestedPeopleCount = parseWholeNumber(input.plannedPeopleCount);

        if (requestedPeopleCount === null) {
            return {
                districtRange,
                residentialBuildingRange: buildingRange,
                capacityRange,
                openingPopulationRange: {min: 0, max: 0},
                occupancyRateRange: {min: 0, max: 0},
                usesExactOverride: true,
            };
        }

        return {
            districtRange,
            residentialBuildingRange: buildingRange,
            capacityRange,
            openingPopulationRange: {
                min: Math.min(requestedPeopleCount, capacityRange.min),
                max: Math.min(requestedPeopleCount, capacityRange.max),
            },
            occupancyRateRange: {min: 0, max: 0},
            usesExactOverride: true,
        };
    }

    const occupancyRateRange = getOccupancyRateRange(input);
    const openingPopulationRange = {
        min: Math.max(
            buildingRange.min,
            Math.round(capacityRange.min * occupancyRateRange.min),
        ),
        max: Math.max(
            buildingRange.max,
            Math.round(capacityRange.max * occupancyRateRange.max),
        ),
    };

    return {
        districtRange,
        residentialBuildingRange: buildingRange,
        capacityRange,
        openingPopulationRange,
        occupancyRateRange,
        usesExactOverride: false,
    };
}

export function formatRange(range: Range): string {
    return `${range.min.toLocaleString()} - ${range.max.toLocaleString()}`;
}

export function formatOccupancyRateRange(range: Range): string {
    return `${Math.round(range.min * 100)}% - ${Math.round(range.max * 100)}%`;
}

function parseWholeNumber(value: string): number | null {
    if (!value.trim()) {
        return null;
    }

    const parsed = Number(value);
    return Number.isInteger(parsed) && parsed >= 0
        ? parsed
        : null;
}

function getDistrictCountRange(input: PopulationPlanningInput): Range {
    const baseCount = input.sizeTier === "Small"
        ? 3
        : input.sizeTier === "Large"
            ? 7
            : 5;

    const densityBonus = input.urbanDensity === "Sparse"
        ? 0
        : input.urbanDensity === "Dense"
            ? 2
            : 1;

    const developmentBonus = input.developmentLevel === "Advanced" ? 1 : 0;

    return {
        min: Math.min(10, baseCount + developmentBonus),
        max: Math.min(10, baseCount + developmentBonus + densityBonus),
    };
}

function getResidentialBuildingRange(
    input: PopulationPlanningInput,
    districtRange: Range,
): Range {
    const centralRange = getBuildingCountPerDistrictRange(input, true);
    const outerRange = getBuildingCountPerDistrictRange(input, false);

    return {
        min: centralRange.min + outerRange.min * Math.max(0, districtRange.min - 1),
        max: centralRange.max + outerRange.max * Math.max(0, districtRange.max - 1),
    };
}

function getCapacityRange(
    input: PopulationPlanningInput,
    districtRange: Range,
): Range {
    const centralBuildingRange = getBuildingCountPerDistrictRange(input, true);
    const outerBuildingRange = getBuildingCountPerDistrictRange(input, false);
    const centralCapacityAverageRange = getAverageCapacityPerBuildingRange(input, true);
    const outerCapacityAverageRange = getAverageCapacityPerBuildingRange(input, false);

    const minimumCapacity = Math.round(
        centralBuildingRange.min * centralCapacityAverageRange.min +
        Math.max(0, districtRange.min - 1) * outerBuildingRange.min * outerCapacityAverageRange.min,
    );
    const maximumCapacity = Math.round(
        centralBuildingRange.max * centralCapacityAverageRange.max +
        Math.max(0, districtRange.max - 1) * outerBuildingRange.max * outerCapacityAverageRange.max,
    );

    return {
        min: minimumCapacity,
        max: maximumCapacity,
    };
}

function getBuildingCountPerDistrictRange(
    input: PopulationPlanningInput,
    isCentral: boolean,
): Range {
    const baseCount = input.urbanDensity === "Sparse"
        ? 2
        : input.urbanDensity === "Dense"
            ? 4
            : 3;

    const sizeBonus = input.sizeTier === "Small"
        ? 0
        : input.sizeTier === "Large"
            ? 2
            : 1;

    const centralBonus = isCentral ? 2 : 0;
    const developmentBonus = input.developmentLevel === "Advanced" ? 1 : 0;
    const minimum = Math.min(9, baseCount + sizeBonus + centralBonus + developmentBonus);

    return {
        min: minimum,
        max: Math.min(9, minimum + 1),
    };
}

function getAverageCapacityPerBuildingRange(
    input: PopulationPlanningInput,
    isCentral: boolean,
): Range {
    const weights = getBuildingTypeWeights(input, isCentral);
    const weightTotal = Object.values(weights).reduce((total, value) => total + value, 0);
    const densityFactor = input.urbanDensity === "Sparse"
        ? 0.85
        : input.urbanDensity === "Dense"
            ? 1.2
            : 1.0;
    const developmentFactor = input.developmentLevel === "Struggling"
        ? 0.9
        : input.developmentLevel === "Advanced"
            ? 1.15
            : 1.0;
    const centralFactor = isCentral ? 1.1 : 1.0;
    const factor = densityFactor * developmentFactor * centralFactor;

    let minimumAverage = 0;
    let maximumAverage = 0;

    (Object.keys(weights) as BuildingType[]).forEach((type) => {
        minimumAverage += BUILDING_CAPACITY_RANGES[type].min * weights[type];
        maximumAverage += BUILDING_CAPACITY_RANGES[type].max * weights[type];
    });

    return {
        min: Math.round((minimumAverage / weightTotal) * factor),
        max: Math.round((maximumAverage / weightTotal) * factor),
    };
}

function getBuildingTypeWeights(
    input: PopulationPlanningInput,
    isCentral: boolean,
): BuildingWeights {
    let houseWeight: number;
    let apartmentWeight: number;
    let towerWeight: number;
    let dormitoryWeight: number;

    switch (input.urbanDensity) {
        case "Sparse":
            houseWeight = 60;
            apartmentWeight = 30;
            towerWeight = 5;
            dormitoryWeight = 5;
            break;
        case "Dense":
            houseWeight = 10;
            apartmentWeight = 45;
            towerWeight = 35;
            dormitoryWeight = 10;
            break;
        default:
            houseWeight = 25;
            apartmentWeight = 50;
            towerWeight = 20;
            dormitoryWeight = 5;
            break;
    }

    if (isCentral) {
        houseWeight = Math.max(2, houseWeight - 15);
        apartmentWeight += 5;
        towerWeight += 10;
    }

    if (input.developmentLevel === "Struggling") {
        apartmentWeight += 10;
        towerWeight = Math.max(2, towerWeight - 10);
    } else if (input.developmentLevel === "Advanced") {
        towerWeight += 10;
        houseWeight = Math.max(2, houseWeight - 5);
    }

    if (input.sizeTier === "Small" && !isCentral) {
        towerWeight = Math.max(1, towerWeight - 10);
    }

    return {
        house: houseWeight,
        apartment: apartmentWeight,
        tower: towerWeight,
        dormitory: dormitoryWeight,
    };
}

function getOccupancyRateRange(input: PopulationPlanningInput): Range {
    const baseOccupancy = getBaseOccupancy(input.populationOccupancyProfile);
    const densityAdjustment = input.urbanDensity === "Sparse"
        ? -0.06
        : input.urbanDensity === "Dense"
            ? 0.06
            : 0.0;
    const developmentAdjustment = input.developmentLevel === "Struggling"
        ? -0.05
        : input.developmentLevel === "Advanced"
            ? 0.04
            : 0.0;

    const baseline = baseOccupancy + densityAdjustment + developmentAdjustment;

    return {
        min: clamp(baseline - 0.04, 0.30, 0.95),
        max: clamp(baseline + 0.04, 0.30, 0.95),
    };
}

function getBaseOccupancy(profile: ClassicCityPopulationOccupancyProfile): number {
    switch (profile) {
        case "Light":
            return 0.44;
        case "High":
            return 0.82;
        default:
            return 0.63;
    }
}

function clamp(value: number, min: number, max: number): number {
    return Math.min(max, Math.max(min, value));
}
