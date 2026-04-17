import type {
    ClassicCityPopulationOccupancyProfile,
    ClassicCityPopulationTargetMode,
    ClassicCitySetupDraftView,
} from "@services/simulationcore/scenarios/classic-city/contracts/setupSessionContracts";

type Range = {
    min: number;
    max: number;
};

type BuildingType = "house" | "apartment" | "tower" | "dormitory";

type BuildingWeights = Record<BuildingType, number>;

export type PopulationPlanningEstimate = {
    targetPopulation: number | null;
    districtRange: Range;
    residentialBuildingRange: Range;
    capacityRange: Range;
    housingCoverageRange: Range;
    housingHeadroomRange: Range;
    usesManualTarget: boolean;
    usesDeterministicRandomTarget: boolean;
};

type PopulationPlanningInput = Pick<
    ClassicCitySetupDraftView,
    | "generationSeed"
    | "populationTargetMode"
    | "plannedPeopleCount"
    | "sizeTier"
    | "urbanDensity"
    | "developmentLevel"
    | "populationOccupancyProfile"
>;

const BUILDING_CAPACITY_RANGES: Record<BuildingType, Range> = {
    house: {min: 4, max: 10},
    apartment: {min: 70, max: 180},
    tower: {min: 180, max: 360},
    dormitory: {min: 120, max: 260},
};

const POPULATION_TARGET_PRESET_COUNTS: Record<Exclude<ClassicCityPopulationTargetMode, "Random" | "Manual">, number> = {
    Preset1K: 1_000,
    Preset10K: 10_000,
    Preset100K: 100_000,
};

export function buildPopulationPlanningEstimate(input: PopulationPlanningInput): PopulationPlanningEstimate {
    const targetPopulation = resolvePopulationTarget(input);
    const topologyPopulationBasis = getTopologyPopulationBasis(input, targetPopulation);
    const capacityRange = getCapacityTargetRange(input, topologyPopulationBasis);
    const districtRange = getDistrictCountRange(input, capacityRange);
    const residentialBuildingRange = getResidentialBuildingRange(input, targetPopulation, capacityRange, districtRange);
    const housingCoverageRange = getHousingCoverageRange(targetPopulation, capacityRange);
    const housingHeadroomRange = getHousingHeadroomRange(targetPopulation, capacityRange);

    return {
        targetPopulation,
        districtRange,
        residentialBuildingRange,
        capacityRange,
        housingCoverageRange,
        housingHeadroomRange,
        usesManualTarget: input.populationTargetMode === "Manual",
        usesDeterministicRandomTarget: input.populationTargetMode === "Random",
    };
}

export function resolvePopulationTarget(input: PopulationPlanningInput): number | null {
    switch (input.populationTargetMode) {
        case "Preset1K":
            return POPULATION_TARGET_PRESET_COUNTS.Preset1K;
        case "Preset10K":
            return POPULATION_TARGET_PRESET_COUNTS.Preset10K;
        case "Preset100K":
            return POPULATION_TARGET_PRESET_COUNTS.Preset100K;
        case "Random":
            return resolveDeterministicRandomTarget(input.generationSeed);
        case "Manual":
            return parseWholeNumber(input.plannedPeopleCount);
        default:
            return POPULATION_TARGET_PRESET_COUNTS.Preset10K;
    }
}

export function getPopulationTargetModeLabel(
    mode: ClassicCityPopulationTargetMode,
    targetPopulation: number | null,
): string {
    switch (mode) {
        case "Random":
            return targetPopulation === null
                ? "Randomized opening"
                : `Randomized opening (${targetPopulation.toLocaleString()} residents)`;
        case "Preset1K":
            return "1,000 residents";
        case "Preset10K":
            return "10,000 residents";
        case "Preset100K":
            return "100,000 residents";
        case "Manual":
            return targetPopulation === null
                ? "Manual resident target"
                : `Manual target (${targetPopulation.toLocaleString()} residents)`;
        default:
            return "Population target";
    }
}

export function getPopulationPressureLabel(profile: ClassicCityPopulationOccupancyProfile): string {
    switch (profile) {
        case "Light":
            return "Room to grow";
        case "High":
            return "Tight housing";
        default:
            return "Balanced housing";
    }
}

export function formatRange(range: Range): string {
    if (range.min === range.max) {
        return range.min.toLocaleString();
    }

    return `${range.min.toLocaleString()} - ${range.max.toLocaleString()}`;
}

export function formatOccupancyRateRange(range: Range): string {
    if (range.min === range.max) {
        return `${Math.round(range.min * 100)}%`;
    }

    return `${Math.round(range.min * 100)}% - ${Math.round(range.max * 100)}%`;
}

export function hasMeaningfulRangeValue(range: Range): boolean {
    return range.max > 0;
}

function getTopologyPopulationBasis(
    input: PopulationPlanningInput,
    targetPopulation: number | null,
): number {
    const fallbackFloor = input.sizeTier === "Small"
        ? 350
        : input.sizeTier === "Large"
            ? 1_600
            : 800;

    return Math.max(targetPopulation ?? 0, fallbackFloor);
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

function getCapacityTargetRange(
    input: PopulationPlanningInput,
    populationBasis: number,
): Range {
    if (populationBasis <= 0) {
        return {min: 0, max: 0};
    }

    const ratioCenter = clamp(
        getBaseHousingRatio(input.populationOccupancyProfile) +
        getDensityHousingModifier(input.urbanDensity) +
        getDevelopmentHousingModifier(input.developmentLevel) +
        getFootprintHousingModifier(input.sizeTier),
        0.75,
        1.45,
    );

    const jitter = 0.08;
    const minRatio = clamp(ratioCenter - jitter, 0.75, 1.45);
    const maxRatio = clamp(ratioCenter + jitter, 0.75, 1.45);

    return {
        min: Math.max(1, Math.round(populationBasis * minRatio)),
        max: Math.max(1, Math.round(populationBasis * maxRatio)),
    };
}

function getDistrictCountRange(
    input: PopulationPlanningInput,
    capacityRange: Range,
): Range {
    const minDistricts = input.sizeTier === "Small"
        ? 3
        : input.sizeTier === "Large"
            ? 5
            : 4;

    const baseCapacityPerDistrict = input.sizeTier === "Small"
        ? 1_200
        : input.sizeTier === "Large"
            ? 4_500
            : 2_500;

    const densityFactor = input.urbanDensity === "Sparse"
        ? 0.75
        : input.urbanDensity === "Dense"
            ? 1.35
            : 1.0;
    const developmentFactor = input.developmentLevel === "Struggling"
        ? 0.9
        : input.developmentLevel === "Advanced"
            ? 1.1
            : 1.0;
    const perDistrictBaseline = baseCapacityPerDistrict * densityFactor * developmentFactor;
    const minCapacityPerDistrict = Math.max(250, Math.round(perDistrictBaseline * 0.9));
    const maxCapacityPerDistrict = Math.max(300, Math.round(perDistrictBaseline * 1.1));

    return {
        min: clamp(Math.ceil(capacityRange.min / maxCapacityPerDistrict), minDistricts, 36),
        max: clamp(Math.ceil(capacityRange.max / minCapacityPerDistrict), minDistricts, 36),
    };
}

function getResidentialBuildingRange(
    input: PopulationPlanningInput,
    targetPopulation: number | null,
    capacityRange: Range,
    districtRange: Range,
): Range {
    const averageBuildingCapacityRange = getAverageBuildingCapacityRange(input, targetPopulation);
    const minBuildings = Math.max(
        districtRange.min,
        Math.ceil(capacityRange.min / Math.max(1, averageBuildingCapacityRange.max)),
    );
    const maxBuildings = Math.max(
        districtRange.max,
        Math.ceil(capacityRange.max / Math.max(1, averageBuildingCapacityRange.min)),
    );

    return {
        min: minBuildings,
        max: maxBuildings,
    };
}

function getAverageBuildingCapacityRange(
    input: PopulationPlanningInput,
    targetPopulation: number | null,
): Range {
    const targetForScale = targetPopulation ?? getTopologyPopulationBasis(input, targetPopulation);
    const centralWeights = getBuildingTypeWeights(input, true);
    const outerWeights = getBuildingTypeWeights(input, false);
    const centralAverage = getWeightedAverageCapacity(centralWeights);
    const outerAverage = getWeightedAverageCapacity(outerWeights);
    const baseAverage = {
        min: Math.round((centralAverage.min + outerAverage.min * 3) / 4),
        max: Math.round((centralAverage.max + outerAverage.max * 3) / 4),
    };

    const scaleCenter = getResidentCapacityScaleCenter(input, targetForScale);

    return {
        min: Math.max(1, Math.round(baseAverage.min * clamp(scaleCenter - 0.08, 0.85, 1.8))),
        max: Math.max(1, Math.round(baseAverage.max * clamp(scaleCenter + 0.08, 0.85, 1.8))),
    };
}

function getWeightedAverageCapacity(weights: BuildingWeights): Range {
    const weightTotal = Object.values(weights).reduce((total, value) => total + value, 0);
    let minimumAverage = 0;
    let maximumAverage = 0;

    (Object.keys(weights) as BuildingType[]).forEach((type) => {
        minimumAverage += BUILDING_CAPACITY_RANGES[type].min * weights[type];
        maximumAverage += BUILDING_CAPACITY_RANGES[type].max * weights[type];
    });

    return {
        min: Math.round(minimumAverage / weightTotal),
        max: Math.round(maximumAverage / weightTotal),
    };
}

function getHousingCoverageRange(
    targetPopulation: number | null,
    capacityRange: Range,
): Range {
    if (targetPopulation === null || targetPopulation <= 0 || capacityRange.max <= 0) {
        return {min: 0, max: 0};
    }

    return {
        min: roundRatio(Math.min(1, capacityRange.min / targetPopulation)),
        max: roundRatio(Math.min(1, capacityRange.max / targetPopulation)),
    };
}

function getHousingHeadroomRange(
    targetPopulation: number | null,
    capacityRange: Range,
): Range {
    if (targetPopulation === null || targetPopulation <= 0 || capacityRange.max <= 0) {
        return {min: 0, max: 0};
    }

    return {
        min: roundRatio(Math.max(0, capacityRange.min / targetPopulation - 1)),
        max: roundRatio(Math.max(0, capacityRange.max / targetPopulation - 1)),
    };
}

function getBaseHousingRatio(profile: ClassicCityPopulationOccupancyProfile): number {
    switch (profile) {
        case "Light":
            return 1.28;
        case "High":
            return 0.92;
        default:
            return 1.08;
    }
}

function getDensityHousingModifier(urbanDensity: string): number {
    switch (urbanDensity) {
        case "Sparse":
            return 0.08;
        case "Dense":
            return -0.05;
        default:
            return 0.0;
    }
}

function getDevelopmentHousingModifier(developmentLevel: string): number {
    switch (developmentLevel) {
        case "Struggling":
            return -0.07;
        case "Advanced":
            return 0.05;
        default:
            return 0.0;
    }
}

function getFootprintHousingModifier(sizeTier: string): number {
    switch (sizeTier) {
        case "Small":
            return -0.03;
        case "Large":
            return 0.04;
        default:
            return 0.0;
    }
}

function getResidentCapacityScaleCenter(
    input: PopulationPlanningInput,
    targetPopulation: number,
): number {
    let scale = 1.0;

    if (targetPopulation >= 100_000) {
        scale += 0.35;
    } else if (targetPopulation >= 10_000) {
        scale += 0.18;
    } else if (targetPopulation >= 1_000) {
        scale += 0.05;
    }

    if (input.urbanDensity === "Dense") {
        scale += 0.10;
    } else if (input.urbanDensity === "Sparse") {
        scale -= 0.05;
    }

    if (input.developmentLevel === "Advanced") {
        scale += 0.08;
    } else if (input.developmentLevel === "Struggling") {
        scale -= 0.05;
    }

    if (input.sizeTier === "Large") {
        scale += 0.06;
    } else if (input.sizeTier === "Small") {
        scale -= 0.03;
    }

    return clamp(scale, 0.85, 1.8);
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

function resolveDeterministicRandomTarget(generationSeed: string): number {
    const hash = fnv1a32(`${generationSeed.trim()}|classic-city|population-target`);
    const anchors = [1_000, 10_000, 100_000] as const;
    const anchor = anchors[hash % anchors.length];
    const jitterPercent = ((Math.floor(hash / anchors.length) % 41) - 20) / 100;
    const rawTarget = Math.max(100, Math.round(anchor * (1 + jitterPercent)));

    if (rawTarget >= 100_000) {
        return roundTo(rawTarget, 1_000);
    }

    if (rawTarget >= 10_000) {
        return roundTo(rawTarget, 100);
    }

    return roundTo(rawTarget, 10);
}

function fnv1a32(value: string): number {
    const bytes = new TextEncoder().encode(value);
    let hash = 0x811c9dc5;

    bytes.forEach((byte) => {
        hash ^= byte;
        hash = Math.imul(hash, 0x01000193) >>> 0;
    });

    return hash >>> 0;
}

function roundTo(value: number, step: number): number {
    return Math.max(step, Math.round(value / step) * step);
}

function roundRatio(value: number): number {
    return Math.round(value * 100) / 100;
}

function clamp(value: number, min: number, max: number): number {
    return Math.min(max, Math.max(min, value));
}
