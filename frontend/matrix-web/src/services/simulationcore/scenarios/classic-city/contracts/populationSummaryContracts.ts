export interface CityPopulationSummaryLifecycleView {
    isArchived: boolean;
    archivedAtUtc?: string | null;
    isDeleted: boolean;
    deletedAtUtc?: string | null;
}

export interface CityPopulationSummaryEnvironmentView {
    climateZone: string;
    hemisphere: string;
    utcOffsetMinutes: number;
    updatedAtUtc: string;
}

export interface CityPopulationSummarySimulationView {
    lastProcessedTickId: number;
    lastProcessedDate: string;
    updatedAtUtc: string;
}

export interface CityPopulationSummaryWeatherView {
    currentType: string;
    currentSeverity: string;
    isRecoveryActive: boolean;
    currentWeatherEffectiveAtSimTimeUtc: string;
    lastWeatherOccurredOnUtc: string;
    lastExposureProcessedAtSimTimeUtc: string;
    lastWeatherImpactAppliedAtSimTimeUtc?: string | null;
}

export interface CityPopulationSummaryHousingView {
    householdCount: number;
    housedHouseholdCount: number;
    homelessHouseholdCount: number;
}

export interface CityPopulationSummaryResidentsView {
    residentCount: number;
    deceasedCount: number;
    housedResidentCount: number;
    homelessResidentCount: number;
    childCount: number;
    youthCount: number;
    adultCount: number;
    seniorCount: number;
    employedCount: number;
    studentCount: number;
    unemployedCount: number;
    retiredCount: number;
    averageHealth?: number | null;
    averageHappiness?: number | null;
    averageEnergy?: number | null;
    averageStress?: number | null;
    averageSocialNeed?: number | null;
}

export interface CityPopulationSummaryView {
    cityId: string;
    currentDate: string;
    lifecycle: CityPopulationSummaryLifecycleView;
    environment?: CityPopulationSummaryEnvironmentView | null;
    simulation?: CityPopulationSummarySimulationView | null;
    weather?: CityPopulationSummaryWeatherView | null;
    housing: CityPopulationSummaryHousingView;
    residents: CityPopulationSummaryResidentsView;
}
