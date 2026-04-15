export interface CityDistrictHeatingConditionView {
    districtId: string;
    heatingCoverageIndex: number;
    heatingSupportIndex: number;
    outageRiskIndex: number;
    comfortStressIndex: number;
    maintenancePriorityIndex: number;
}

export interface CityDistrictHeatingConditionsView {
    cityId: string;
    effectiveTickId: number;
    lastEvaluatedAtUtc: string;
    heatingSupportIndex: number;
    districts: CityDistrictHeatingConditionView[];
}

export interface CityDistrictWaterDistributionConditionView {
    districtId: string;
    waterCoverageIndex: number;
    waterSupportIndex: number;
    disruptionRiskIndex: number;
    qualityRiskIndex: number;
    maintenancePriorityIndex: number;
}

export interface CityDistrictWaterDistributionConditionsView {
    cityId: string;
    effectiveTickId: number;
    lastEvaluatedAtUtc: string;
    waterSupportIndex: number;
    districts: CityDistrictWaterDistributionConditionView[];
}

export interface CityDistrictPowerDistributionConditionView {
    districtId: string;
    powerCoverageIndex: number;
    powerSupportIndex: number;
    outageRiskIndex: number;
    restorationStrainIndex: number;
    maintenancePriorityIndex: number;
}

export interface CityDistrictPowerDistributionConditionsView {
    cityId: string;
    effectiveTickId: number;
    lastEvaluatedAtUtc: string;
    powerSupportIndex: number;
    districts: CityDistrictPowerDistributionConditionView[];
}

export interface CityDistrictSanitationConditionView {
    districtId: string;
    sanitationCoverageIndex: number;
    sanitationSupportIndex: number;
    overflowRiskIndex: number;
    contaminationRiskIndex: number;
    maintenancePriorityIndex: number;
}

export interface CityDistrictSanitationConditionsView {
    cityId: string;
    effectiveTickId: number;
    lastEvaluatedAtUtc: string;
    sanitationSupportIndex: number;
    districts: CityDistrictSanitationConditionView[];
}

export interface CityDistrictUtilityIncidentConditionView {
    districtId: string;
    utilityContinuityIndex: number;
    dispatchReadinessIndex: number;
    incidentPressureIndex: number;
    coordinationDifficultyIndex: number;
    restorationPriorityIndex: number;
}

export interface CityDistrictUtilityIncidentConditionsView {
    cityId: string;
    effectiveTickId: number;
    lastEvaluatedAtUtc: string;
    utilityIncidentSupportIndex: number;
    districts: CityDistrictUtilityIncidentConditionView[];
}

export interface CityDistrictInfrastructureView {
    cityId: string;
    generatedAtUtc: string;
    heating: CityDistrictHeatingConditionsView;
    waterDistribution: CityDistrictWaterDistributionConditionsView;
    powerDistribution: CityDistrictPowerDistributionConditionsView;
    sanitation: CityDistrictSanitationConditionsView;
    utilityIncidents: CityDistrictUtilityIncidentConditionsView;
}
